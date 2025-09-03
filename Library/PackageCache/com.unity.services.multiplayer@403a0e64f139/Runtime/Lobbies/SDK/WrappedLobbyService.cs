using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lobbies.SDK.LobbyCacher;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies.Http;
using Unity.Services.Lobbies.Apis.Lobby;
using Unity.Services.Lobbies.Lobby;
using Unity.Services.Core;
using Unity.Services.Authentication.Internal;
using Unity.Services.Multiplayer;
using Player = Unity.Services.Lobbies.Models.Player;

namespace Unity.Services.Lobbies.Internal
{
    /// <summary>
    /// The Lobby Service enables clients to create/host, join, delete lobbies using the bespoke underlying relay protocol.
    /// </summary>

#pragma warning disable CS0618 // Ignoring warning as we want to implement ILobbyServiceSDK for backwards compatibility.
    internal class WrappedLobbyService : ILobbyService, ILobbyServiceSDK, ILobbyServiceSDKConfiguration, ILobbyServiceInternal
#pragma warning restore CS0618
    {
        const int k_CommonErrorCodeRange = 100;

        internal ILobbyServiceSdk m_LobbyService;

        internal LobbyChannel m_LobbyChannel;

        //Caches lobby data to be able to make diffs against Lobby versions.
        //Refreshed with newer data when using GetLobby, CreateLobby, JoinLobby, UpdateLobby, and UpdatePlayer.
        internal LobbyCacher m_LobbyCacher;

        private readonly ApiTelemetryScopeFactory m_TelemetryScopeFactory;
        readonly IPlayerId m_PlayerId;
        readonly IServiceID m_ServiceId;

        //Minimum value of a lobby error (used to elevate standard errors if unhandled)
        internal const int LOBBY_ERROR_MIN_RANGE = 16000;

        /// <param name="playerId">Use null for UNITY_SERVER</param>
        /// <param name="serviceId">Use null for !UNITY_SERVER</param>
        internal WrappedLobbyService(ILobbyServiceSdk lobbyService, IPlayerId playerId, IServiceID serviceId)
        {
            m_LobbyService = lobbyService;
            m_PlayerId = playerId;
            m_ServiceId = serviceId;
            m_TelemetryScopeFactory = new ApiTelemetryScopeFactory(lobbyService.Metrics);
            m_LobbyCacher = new LobbyCacher(m_PlayerId?.PlayerId);
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, CreateLobbyOptions options = default)
        {
            if (string.IsNullOrWhiteSpace(lobbyName))
            {
                throw new ArgumentNullException("lobbyName", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (maxPlayers < 1)
            {
                throw new InvalidOperationException("Parameters 'maxPlayers' cannot be less than 1.");
            }

            var createRequest = ConvertCreateOptionsToRequest(lobbyName, maxPlayers, options);
            var response = await TryCatchRequest(LobbyApiNames.CreateLobby, m_LobbyService.LobbyApi.CreateLobbyAsync, new CreateLobbyRequest(createRequest : createRequest, serviceId : m_ServiceId?.ServiceID));
            var lobby = response.Result;
            AddOrUpdateLobbyCache(lobby);
            return lobby;
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> CreateOrJoinLobbyAsync(string lobbyId, string lobbyName, int maxPlayers, CreateLobbyOptions createOptions = default)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (string.IsNullOrWhiteSpace(lobbyName))
            {
                throw new ArgumentNullException("lobbyName", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (maxPlayers < 1)
            {
                throw new InvalidOperationException("Parameters 'maxPlayers' cannot be less than 1.");
            }


            var createRequest = ConvertCreateOptionsToRequest(lobbyName, maxPlayers, createOptions);
            var createOrJoinRequest = new CreateOrJoinLobbyRequest(
                lobbyId: lobbyId,
                serviceId: m_ServiceId?.ServiceID,
                createRequest: createRequest
            );

            var response = await TryCatchRequest(LobbyApiNames.CreateOrJoinLobby, m_LobbyService.LobbyApi.CreateOrJoinLobbyAsync, createOrJoinRequest);
            var lobby = response.Result;
            AddOrUpdateLobbyCache(lobby);
            return lobby;
        }

        /// <inheritdoc/>
        public async Task<ILobbyEvents> SubscribeToLobbyEventsAsync(string lobbyId, LobbyEventCallbacks lobbyEventCallbacks)
        {
            if (m_LobbyService.Wire != null)
            {
                var channel = m_LobbyService.Wire.CreateChannel(new LobbyWireTokenProvider(lobbyId, this));
                m_LobbyChannel = new LobbyChannel(m_PlayerId, channel, lobbyEventCallbacks, lobbyId, this);
                GC.SuppressFinalize(m_LobbyChannel);
                await m_LobbyChannel.SubscribeAsync();
                m_LobbyCacher?.WithEventSubscription(m_LobbyChannel.Callbacks);
                return m_LobbyChannel;
            }
            return null;
        }

        public ILobbyEvents SetCacherLobbyCallbacks(string lobbyId, LobbyEventCallbacks lobbyEventCallbacks)
        {
            if (m_LobbyCacher == null)
            {
                return null;
            }

            m_LobbyCacher.WithEventSubscription(lobbyEventCallbacks);

            return m_LobbyCacher;
        }

        /// <inheritdoc/>
        public async Task DeleteLobbyAsync(string lobbyId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }

            await TryCatchRequest(LobbyApiNames.DeleteLobby, m_LobbyService.LobbyApi.DeleteLobbyAsync, new DeleteLobbyRequest(lobbyId, m_ServiceId?.ServiceID));
            m_LobbyCacher.RemoveLobbyCache(lobbyId);
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetJoinedLobbiesAsync()
        {
            var request = new GetJoinedLobbiesRequest(null, null);
            var response = await TryCatchRequest(LobbyApiNames.GetJoinedLobbies, m_LobbyService.LobbyApi.GetJoinedLobbiesAsync, request);
            var lobbyIds = response.Result;
            return lobbyIds;
        }

        /// <inheritdoc/>
        public Task<Models.Lobby> GetLobbyAsync(string lobbyId)
        {
           return GetLobbyAsync(lobbyId, null);
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> GetLobbyAsync(string lobbyId, string ifNoneMatchVersion)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }

            var response = await TryCatchRequest(LobbyApiNames.GetLobby, m_LobbyService.LobbyApi.GetLobbyAsync, new GetLobbyRequest(lobbyId, m_ServiceId?.ServiceID, ifNoneMatch: ifNoneMatchVersion));
            if (response.Result != null)
            {
                AddOrUpdateLobbyCache(response.Result);
            }
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task SendHeartbeatPingAsync(string lobbyId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }

            await TryCatchRequest(LobbyApiNames.Heartbeat, m_LobbyService.LobbyApi.HeartbeatAsync, new HeartbeatRequest(lobbyId, m_ServiceId?.ServiceID));
        }

        public async Task<Models.Lobby> JoinLobbyByCodeAsync(string lobbyCode, JoinLobbyByCodeOptions options = default)
        {
            if (string.IsNullOrWhiteSpace(lobbyCode))
            {
                throw new ArgumentNullException("lobbyCode", "Argument should be non-null, non-empty & not only whitespaces.");
            }

            try
            {
                // NOTE: constructor not passing value by name to ensure this breaks on any regeneration that changes the order of existing arguments
                var joinRequest = new JoinLobbyByCodeRequest(joinByCodeRequest: new JoinByCodeRequest(lobbyCode, options?.Player, options?.Password));
                var response = await TryCatchRequest(LobbyApiNames.JoinLobbyByCode, m_LobbyService.LobbyApi.JoinLobbyByCodeAsync, joinRequest);
                AddOrUpdateLobbyCache(response.Result);
                return response.Result;
            }
            catch (LobbyServiceException e)
            {
                //JoinLobby conflict 409 handling (MPSSDK-92)
                if (e.Reason == LobbyExceptionReason.LobbyConflict)
                {
                    var lobby = await LobbyConflictResolver(options?.Player, null, e);
                    if (lobby != null)
                    {
                        AddOrUpdateLobbyCache(lobby);
                        return lobby;
                    }
                }
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> JoinLobbyByIdAsync(string lobbyId, JoinLobbyByIdOptions options = default)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }

            try
            {
                var joinByIdRequest = new JoinByIdRequest(options?.Password, options?.Player);
                var joinRequest = new JoinLobbyByIdRequest(lobbyId, joinByIdRequest: joinByIdRequest);
                var response = await TryCatchRequest(LobbyApiNames.JoinLobbyById, m_LobbyService.LobbyApi.JoinLobbyByIdAsync, joinRequest);
                AddOrUpdateLobbyCache(response.Result);
                return response.Result;
            }
            catch (LobbyServiceException e)
            {
                //JoinLobby conflict 409 handling (MPSSDK-92)
                if (e.Reason == LobbyExceptionReason.LobbyConflict)
                {
                    var lobby = await LobbyConflictResolver(options?.Player, lobbyId, e);
                    if (lobby != null)
                    {
                        AddOrUpdateLobbyCache(lobby);
                        return lobby;
                    }
                }
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<QueryResponse> QueryLobbiesAsync(QueryLobbiesOptions options = default)
        {
            var queryRequest = options == null ? null : new QueryRequest(options.Count, options.Skip, options.SampleResults, options.Filters, options.Order, options.ContinuationToken);
            var queryLobbiesRequest = new QueryLobbiesRequest(queryRequest: queryRequest, serviceId: m_ServiceId?.ServiceID);
            var response = await TryCatchRequest(LobbyApiNames.QueryLobbies, m_LobbyService.LobbyApi.QueryLobbiesAsync, queryLobbiesRequest);
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> QuickJoinLobbyAsync(QuickJoinLobbyOptions options = default)
        {
            try
            {
                var quickJoinRequest = options == null ? null : new QuickJoinRequest(options.Filter, options.Player);
                var quickJoinLobbyRequest = new QuickJoinLobbyRequest(quickJoinRequest: quickJoinRequest);
                var response = await TryCatchRequest(LobbyApiNames.QuickJoinLobby, m_LobbyService.LobbyApi.QuickJoinLobbyAsync, quickJoinLobbyRequest);
                AddOrUpdateLobbyCache(response.Result);
                return response.Result;
            }
            catch (LobbyServiceException e)
            {
                //JoinLobby conflict 409 handling (MPSSDK-92)
                if (e.Reason == LobbyExceptionReason.LobbyConflict)
                {
                    var lobby = await LobbyConflictResolver(options?.Player, null, e);
                    if (lobby != null)
                    {
                        AddOrUpdateLobbyCache(lobby);
                        return lobby;
                    }
                }
                throw;
            }
        }

        /// <inheritdoc/>
        public Task RemovePlayerAsync(string lobbyId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException(nameof(lobbyId), "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentNullException(nameof(playerId), "Argument should be non-null, non-empty & not only whitespaces.");
            }

            var impersonatedUserId = default(string);
            // If we are using service authentication, we can remove a player other than ourselves with the impersonatedUserId header.
            if (m_ServiceId?.ServiceID != null && m_PlayerId?.PlayerId != playerId)
            {
                impersonatedUserId = playerId;
            }

            var removePlayerRequest = new RemovePlayerRequest(lobbyId, playerId, m_ServiceId?.ServiceID, impersonatedUserId);

            return RemovePlayerTask();

            async Task RemovePlayerTask()
            {
                try
                {
                    await TryCatchRequest(LobbyApiNames.RemovePlayer, m_LobbyService.LobbyApi.RemovePlayerAsync,
                        removePlayerRequest);
                }
                catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.PlayerNotFound)
                {
                    Logger.LogVerbose($"The player with id '{playerId}' could not be removed because it was not found in the lobby with id '{lobbyId}'.");
                }
            }
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> UpdateLobbyAsync(string lobbyId, UpdateLobbyOptions options)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "Update Lobby Options object must not be null.");
            }

            var updateRequest = new UpdateRequest(options.Name, options.MaxPlayers, options.IsPrivate, options.IsLocked, options.Data, options.HostId, options.Password);
            var updateLobbyRequest = new UpdateLobbyRequest(lobbyId, updateRequest: updateRequest, serviceId: m_ServiceId?.ServiceID);
            var response = await TryCatchRequest(LobbyApiNames.UpdateLobby, m_LobbyService.LobbyApi.UpdateLobbyAsync, updateLobbyRequest);
            AddOrUpdateLobbyCache(response.Result);
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> UpdatePlayerAsync(string lobbyId, string playerId, UpdatePlayerOptions options)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException(nameof(lobbyId), "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentNullException(nameof(playerId), "Argument should be non-null, non-empty & not only whitespaces.");
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "Update player options object must not be null.");
            }
            var playerUpdateRequest = new PlayerUpdateRequest(options.ConnectionInfo, options.Data, options.AllocationId);
            var updatePlayerRequest = new UpdatePlayerRequest(lobbyId, playerId, playerUpdateRequest: playerUpdateRequest, serviceId: m_ServiceId?.ServiceID);
            var response = await TryCatchRequest(LobbyApiNames.UpdatePlayer, m_LobbyService.LobbyApi.UpdatePlayerAsync, updatePlayerRequest);
            AddOrUpdateLobbyCache(response.Result);
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task<Models.Lobby> ReconnectToLobbyAsync(string lobbyId)
        {
            if (string.IsNullOrWhiteSpace(lobbyId))
            {
                throw new ArgumentNullException("lobbyId", "Argument should be non-null, non-empty & not only whitespaces.");
            }
            var reconnectRequest = new ReconnectRequest(lobbyId, m_ServiceId?.ServiceID);
            var response = await TryCatchRequest(LobbyApiNames.Reconnect, m_LobbyService.LobbyApi.ReconnectAsync, reconnectRequest);
            AddOrUpdateLobbyCache(response.Result);
            return response.Result;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, Models.TokenData>> RequestTokensAsync(string lobbyId, params TokenRequest.TokenTypeOptions[] tokenOptions)
        {
            if (tokenOptions == null || tokenOptions.Length < 1)
            {
                throw new ArgumentNullException("Unable to request tokens when no token options were chosen to receive from the request!");
            }
            var tokenRequestOptions = new List<TokenRequest>(tokenOptions.Length);
            foreach (var tokenOption in tokenOptions)
            {
                tokenRequestOptions.Add(new TokenRequest(tokenOption));
            }
            var requestTokensRequest = new RequestTokensRequest(lobbyId, tokenRequestOptions);
            var response = await TryCatchRequest(LobbyApiNames.RequestTokens, m_LobbyService.LobbyApi.RequestTokensAsync, requestTokensRequest);
            return response.Result;
        }

        public void SetBasePath(string basePath)
        {
            m_LobbyService.Configuration.BasePath = basePath;
        }

        #region Helper Functions

        // Helper function to reduce code duplication of try-catch
        private async Task<Response> TryCatchRequest<TRequest>(string api, Func<TRequest, Configuration, Task<Response>> func, TRequest request)
        {
            Response response = null;
            try
            {
                using (m_TelemetryScopeFactory.Instrument(api))
                {
                    response = await func(request, m_LobbyService.Configuration);
                }
            }
            catch (HttpException<ErrorStatus> he)
            {
                ResolveErrorWrapping((LobbyExceptionReason)he.ActualError.Code, he);
            }
            catch (HttpException he)
            {
                int httpErrorStatusCode = (int)he.Response.StatusCode;
                LobbyExceptionReason reason = LobbyExceptionReason.Unknown;
                if (he.Response.IsNetworkError)
                {
                    reason = LobbyExceptionReason.NetworkError;
                }
                else if (he.Response.IsHttpError)
                {
                    //Elevate unhandled http codes to lobby enum range
                    if (httpErrorStatusCode < 1000)
                    {
                        httpErrorStatusCode += LOBBY_ERROR_MIN_RANGE;
                        if (Enum.IsDefined(typeof(LobbyExceptionReason), httpErrorStatusCode))
                        {
                            reason = (LobbyExceptionReason)httpErrorStatusCode;
                        }
                    }
                }

                ResolveErrorWrapping(reason, he);
            }
            catch (LobbyServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                //Pass error code that will throw default label, provide exception object for stack trace.
                ResolveErrorWrapping(LobbyExceptionReason.Unknown, e);
            }

            return response;
        }

        // Helper function to reduce code duplication of try-catch (generic version)
        private async Task<Response<TReturn>> TryCatchRequest<TRequest, TReturn>(string api, Func<TRequest, Configuration, Task<Response<TReturn>>> func, TRequest request)
        {
            Response<TReturn> response = null;
            try
            {
                using (m_TelemetryScopeFactory.Instrument(api))
                {
                    response = await func(request, m_LobbyService.Configuration);
                }
            }
            catch (HttpException<ErrorStatus> he)
            {
                ResolveErrorWrapping((LobbyExceptionReason)he.ActualError.Code, he);
            }
            catch (HttpException he)
            {
                int httpErrorStatusCode = (int)he.Response.StatusCode;
                LobbyExceptionReason reason = LobbyExceptionReason.Unknown;
                if (he.Response.IsNetworkError)
                {
                    reason = LobbyExceptionReason.NetworkError;
                }
                else if (he.Response.IsHttpError)
                {
                    //Elevate unhandled http codes to lobby enum range
                    if (httpErrorStatusCode < 1000)
                    {
                        httpErrorStatusCode += LOBBY_ERROR_MIN_RANGE;
                        if (Enum.IsDefined(typeof(LobbyExceptionReason), httpErrorStatusCode))
                        {
                            reason = (LobbyExceptionReason)httpErrorStatusCode;
                        }
                    }
                }

                ResolveErrorWrapping(reason, he);
            }
            catch (LobbyServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                //Pass error code that will throw default label, provide exception object for stack trace.
                ResolveErrorWrapping(LobbyExceptionReason.Unknown, e);
            }

            return response;
        }

        // Helper function to resolve the new wrapped error/exception based on input parameter
        private void ResolveErrorWrapping(LobbyExceptionReason reason, Exception exception = null)
        {
            if (reason == LobbyExceptionReason.Unknown)
            {
                throw new LobbyServiceException(reason, "Something went wrong.", exception);
            }

            if (TryMapCommonErrorCodeToLobbyExceptionReason((int)reason, out var mappedReason))
            {
                reason = mappedReason;
            }

            throw ConvertToLobbyServiceException(reason, exception);
        }

        /// <summary>
        /// Converts an exception into a <see cref="LobbyServiceException" />
        /// with the specified <see cref="LobbyExceptionReason">reason</see>.
        /// </summary>
        /// <param name="reason">The reason for the exception, represented as a
        /// <see cref="LobbyExceptionReason"/>.</param>
        /// <param name="exception">The original exception to be
        /// converted.</param>
        /// <returns>A <see cref="LobbyServiceException"/> containing the
        /// provided <see cref="LobbyExceptionReason">reason</see> and details
        /// from the original exception.</returns>
        /// <remarks>
        /// If the original exception is of type <see cref="HttpException{T}"/>,
        /// it extracts the user-facing message and appends any additional
        /// details to the message. For other exceptions, it uses the original
        /// exception's message or an empty string if the original exception is
        /// <see langword="null"/>.
        /// </remarks>
        static LobbyServiceException ConvertToLobbyServiceException(
            LobbyExceptionReason reason, Exception exception)
        {
            if (exception is not HttpException<ErrorStatus> apiException)
            {
                return new LobbyServiceException(reason,
                    exception?.Message ?? string.Empty, exception);
            }

            var message = apiException.ActualError.Detail;
            if (apiException.ActualError?.Details is { Count: > 0 } details)
            {
                var sb = new StringBuilder(apiException.ActualError.Detail, details.Sum(d => d.Message.Length));
                for (var index = 0; index < details.Count; ++index)
                {
                    var detail = details[index];
                    sb.AppendLine();
                    sb.Append(detail.Message);
                    if (index < details.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }

                message = sb.ToString();
            }

            return new LobbyServiceException(reason, message, apiException);
        }

        static bool TryMapCommonErrorCodeToLobbyExceptionReason(int code, out LobbyExceptionReason reason)
        {
            if (code < k_CommonErrorCodeRange)
            {
                switch (code)
                {
                    case CommonErrorCodes.Unknown: reason = LobbyExceptionReason.Unknown; break;
                    case CommonErrorCodes.ServiceUnavailable: reason = LobbyExceptionReason.ServiceUnavailable; break;
                    case CommonErrorCodes.TooManyRequests: reason = LobbyExceptionReason.RateLimited; break;
                    case CommonErrorCodes.Forbidden: reason = LobbyExceptionReason.Forbidden; break;
                    case CommonErrorCodes.NotFound: reason = LobbyExceptionReason.EntityNotFound; break;
                    case CommonErrorCodes.InvalidRequest: reason = LobbyExceptionReason.BadRequest; break;
                    default: reason = LobbyExceptionReason.UnknownErrorCode; break;
                }
                return true;
            }
            reason = LobbyExceptionReason.Unknown;
            return false;
        }

        /// <summary>
        /// Helper function to resolve lobby conflicts due to potentially lost responses (MPSSDK-92)
        /// N.B. lobbyId will need to be inferred from GetLobby if parameter is invalid (this may have unintended effects if the user has joined multiple lobbies)
        /// </summary>
        /// <param name="player">Player data to update with if any data mismatches are encountered</param>
        /// <param name="lobbyId">(Optional) target lobbyId that the request failed on</param>
        /// <param name="e">(Optional) exception to nest as innerException if new Exception is thrown</param>
        /// <returns>Lobby currently joined (amending any mismatched data), otherwise null</returns>
        private async Task<Models.Lobby> LobbyConflictResolver(Player player, string lobbyId = default, LobbyServiceException e = null)
        {
            List<string> joinedLobbies;
            if (!string.IsNullOrWhiteSpace(lobbyId))
            {
                joinedLobbies = await GetJoinedLobbiesAsync();
                if (joinedLobbies.Count != 1)
                {
                    return null;
                }

                lobbyId = joinedLobbies[0];
            }

            //If lobbyId is still null, we were unable to find a corresponding lobby through GetJoinedLobbiesAsync
            if (lobbyId == null)
            {
                return null;
            }

            //Call GetLobby to get existing details
            Models.Lobby getLobbyResult = await GetLobbyAsync(lobbyId);

            //Check to see we have a valid lobby and a valid player object for amending data.
            //N.B. We do not validate anything beyond PlayerId in the Player object.
            if (getLobbyResult == null || player?.Id == null)
            {
                return getLobbyResult;
            }

            var lobbyPlayers = getLobbyResult.Players;
            var playerObjectInLobby = lobbyPlayers.FirstOrDefault(x => x.Id == player.Id);
            if (playerObjectInLobby == null)
            {
                throw new LobbyServiceException(LobbyExceptionReason.PlayerNotFound, "Lobby join call failed and player was not added to Lobby due to an unexpected error.", e);
            }

            //if player is part of the lobby and their details don't match (e.g. different relay alloc id), call update player to update details
            if (IsPlayerDataEqual(player, playerObjectInLobby))
            {
                return getLobbyResult;
            }

            //Update with the details that was attempted to send (Lobby ones may be outdated).
            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                ConnectionInfo = player.ConnectionInfo,
                Data = player.Data,
                AllocationId = player.AllocationId
            };

            return await UpdatePlayerAsync(lobbyId, player.Id, options);
        }

        // Helper method for determining if a Player object is equal to another.
        // N.B. Player is a generated class which makes overriding Object.Equals a sub-optimal approach.
        private bool IsPlayerDataEqual(Player a, Player b)
        {
            bool result = (a.Id == b.Id);
            result &= (a.ConnectionInfo == b.ConnectionInfo);
            result &= (a.AllocationId == b.AllocationId);
            result &= (a.Joined == b.Joined);
            result &= (a.LastUpdated == b.LastUpdated);

            var aKeys = a.Data.Keys;
            var bKeys = b.Data.Keys;
            bool areDictKeysEqual =
                aKeys.All(bKeys.Contains) && aKeys.Count == bKeys.Count;
            result &= areDictKeysEqual;

            //Early exit opeprtunity here before checking individual data
            if (!result)
            {
                return false;
            }

            foreach (string key in aKeys)
            {
                var aPlayerDataObject = a.Data[key];
                var bPlayerDataObject = b.Data[key];
                result &= (aPlayerDataObject.Value == bPlayerDataObject.Value);
                result &= (aPlayerDataObject.Visibility == bPlayerDataObject.Visibility);
            }

            return result;
        }

        internal void AddOrUpdateLobbyCache(Models.Lobby newLobby)
        {
            if (!m_LobbyCacher.TryGetLobbyCache(newLobby.Id, out _))
            {
                // Pass a copy of 'newLobby' so the 'LobbyCacher' and the
                // 'LobbyHandler' hold unique and disjoint lobbies.
                var lobby = CloneLobbyHelper(newLobby);
                m_LobbyCacher.AddLobbyCache(lobby.Id, lobby);
                return;
            }

            m_LobbyCacher.UpdateLobbyCache(newLobby.Id, newLobby);
        }

        internal static Models.Lobby CloneLobbyHelper(Models.Lobby otherLobby)
        {
            var newLobby = new Models.Lobby();

            newLobby.Version = otherLobby.Version;
            newLobby.Id = otherLobby.Id;
            newLobby.Name = otherLobby.Name;
            newLobby.AvailableSlots = otherLobby.AvailableSlots;
            newLobby.HasPassword = otherLobby.HasPassword;

            if (otherLobby.Players != null)
            {
                newLobby.Players = new List<Player>();
                foreach (var player in otherLobby.Players)
                {
                    var newPlayer = new Player()
                    {
                        Id = player.Id,
                        AllocationId = player.AllocationId,
                        Joined = player.Joined,
                        ConnectionInfo = player.ConnectionInfo,
                        LastUpdated = player.LastUpdated,
                        Profile = player.Profile
                    };

                    if (player.Data != null)
                    {
                        newPlayer.Data = new Dictionary<string, PlayerDataObject>();
                        foreach (var data in player.Data)
                        {
                            newPlayer.Data[data.Key] = new PlayerDataObject(data.Value.Visibility, data.Value.Value);
                        }
                    }

                    newLobby.Players.Add(newPlayer);
                }
            }

            if (otherLobby.Data != null)
            {
                newLobby.Data = new Dictionary<string, DataObject>();
                foreach (var data in otherLobby.Data)
                {
                    newLobby.Data[data.Key] = new DataObject(data.Value.Visibility, data.Value.Value, data.Value.Index);
                }
            }

            newLobby.Upid = otherLobby.Upid;
            newLobby.EnvironmentId = otherLobby.EnvironmentId;
            newLobby.HostId = otherLobby.HostId;
            newLobby.IsLocked = otherLobby.IsLocked;
            newLobby.IsPrivate = otherLobby.IsPrivate;
            newLobby.LobbyCode = otherLobby.LobbyCode;
            newLobby.MaxPlayers = otherLobby.MaxPlayers;
            newLobby.Created = otherLobby.Created;
            newLobby.LastUpdated = otherLobby.LastUpdated;

            return newLobby;
        }

        public LobbyCacher GetLobbyCacher()
        {
            return m_LobbyCacher;
        }

        private CreateRequest ConvertCreateOptionsToRequest(string lobbyName, int maxPlayers, CreateLobbyOptions options)
        {
            return new CreateRequest(
                name: lobbyName,
                maxPlayers: maxPlayers,
                isPrivate: options?.IsPrivate,
                isLocked: options?.IsLocked,
                player:  options?.Player,
                data: options?.Data,
                password: options?.Password
            );
        }

        #endregion
    }
}
