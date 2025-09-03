namespace Unity.Services.Multiplayer
{
    /// <summary>
    /// The type of error related to sessions
    /// </summary>
    public enum SessionError
    {
        /// <summary>
        /// Default value.
        /// </summary>
        None = 0,
        /// <summary>
        /// The error returned when the error is undetermined.
        /// </summary>
        Unknown = 1,
        /// <summary>
        /// The error returned when the player is not authorized.
        /// </summary>
        NotAuthorized = 2,
        /// <summary>
        /// The error returned when the player is not in a lobby.
        /// </summary>
        NotInLobby = 3,
        /// <summary>
        /// The error returned when the player is already in a lobby.
        /// </summary>
        LobbyAlreadyExists = 4,
        /// <summary>
        /// The error returned when a session type is already in use.
        /// </summary>
        SessionTypeAlreadyExists = 5,
        /// <summary>
        /// The error returned when the session cannot be found.
        /// </summary>
        SessionNotFound = 6,
        /// <summary>
        /// The error returned when the session has been deleted.
        /// </summary>
        SessionDeleted = 7,
        /// <summary>
        /// The error returned when an allocation already exists.
        /// </summary>
        AllocationAlreadyExists = 8,
        /// <summary>
        /// The error returned when the allocation cannot be found.
        /// </summary>
        AllocationNotFound = 9,
        /// <summary>
        /// The error returned when executing a method without the adequate permissions.
        /// </summary>
        Forbidden = 10,
        /// <summary>
        /// The error returned when exceeding the rate limit.
        /// </summary>
        RateLimitExceeded = 11,
        /// <summary>
        /// The error returned when a parameter is missing or not in the right format.
        /// </summary>
        InvalidParameter = 12,
        /// <summary>
        /// The error returned when a matchmaker ticket is null or empty.
        /// </summary>
        InvalidMatchmakerTicket = 13,
        /// <summary>
        /// The error returned when a matchmaker assignment is invalid.
        /// </summary>
        InvalidMatchmakerAssignment = 14,
        /// <summary>
        /// The error returned when the matchmaker state is invalid for the operation performed.
        /// </summary>
        InvalidMatchmakerState = 15,
        /// <summary>
        /// The error returned when the matchmaker results are invalid.
        /// </summary>
        InvalidMatchmakerResults = 16,
        /// <summary>
        /// The error returned when the network configuration is invalid.
        /// </summary>
        InvalidNetworkConfig = 17,
        /// <summary>
        /// The error returned when the session metadata is invalid.
        /// </summary>
        InvalidSessionMetadata = 18,
        /// <summary>
        /// The error returned when the platform operation is invalid.
        /// </summary>
        InvalidPlatformOperation = 19,
        /// <summary>
        /// The error returned when the operation is invalid.
        /// </summary>
        InvalidOperation = 20,
        /// <summary>
        /// The error returned when the matchmaker assignment fails.
        /// </summary>
        MatchmakerAssignmentFailed = 21,
        /// <summary>
        /// The error returned when the matchmaker assignment times out.
        /// </summary>
        MatchmakerAssignmentTimeout = 22,
        /// <summary>
        /// The error returned when the matchmaker is cancelled.
        /// </summary>
        MatchmakerCancelled = 23,
        /// <summary>
        /// The error returned when the project does not the required dependencies.
        /// </summary>
        MissingAssembly = 24,
        /// <summary>
        /// The error returned by a multiplayer server.
        /// </summary>
        MultiplayServerError = 25,
        /// <summary>
        /// The error returned when network manager fails to initialize.
        /// </summary>
        NetworkManagerNotInitialized = 26,
        /// <summary>
        /// The error returned when network manager fails to start.
        /// </summary>
        NetworkManagerStartFailed = 27,
        /// <summary>
        /// The error returned when the connection fails.
        /// </summary>
        NetworkSetupFailed = 28,
        /// <summary>
        /// The error returned when transport layer component is missing.
        /// </summary>
        TranportComponentMissing = 29, // Keep for backward compatibility purposes.
        /// <summary>
        /// The error returned when transport layer component is missing.
        /// </summary>
        TransportComponentMissing = 29,
        /// <summary>
        /// The error returned when transport layer is invalid.
        /// </summary>
        TransportInvalid = 30,
        /// <summary>
        /// The error returned when the player has already subscribed to the lobby.
        /// </summary>
        AlreadySubscribedToLobby = 31,
        /// <summary>
        /// The error returned when the create session options are invalid.
        /// </summary>
        InvalidCreateSessionOptions = 32,
        /// <summary>
        /// The error returned when the backfill ticket is invalid.
        /// </summary>
        InvalidBackfillTicket = 33,
        /// <summary>
        /// The error returned when the local match properties are invalid.
        /// </summary>
        InvalidLocalMatchProperties = 34,
        /// <summary>
        /// The error returned when the player's team is invalid.
        /// </summary>
        InvalidPlayerTeam = 35,
        /// <summary>
        /// The error returned when the player's team property is missing.
        /// </summary>
        PlayerMissingTeamProperties = 36,
        /// <summary>
        /// The error returned when QoS measurement fails.
        /// </summary>
        QoSMeasurementFailed = 37,
        /// <summary>
        /// The error returned when the matchmaker module is missing.
        /// </summary>
        MatchmakerModuleMissing = 39,
        /// <summary>
        /// The error return when the matchmaker options are invalid.
        /// </summary>
        InvalidMatchmakerOptions = 40,


        /// <summary>
        /// The error returned when the provided session identifier is invalid.
        /// </summary>
        InvalidSessionIdentifier = 42,
    }
}
