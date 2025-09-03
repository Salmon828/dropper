# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.6] - 2025-07-28

### Fixed
- Resolved an issue where a host could not rejoin the same session after leaving while using Distributed Authority.

## [1.1.5] - 2025-07-14

### Fixed
- Fixed an internal task scheduling issue that caused Session Create and Join operations to hang indefinitely on WebGL.

## [1.1.4] - 2025-06-18

### Added
- Added access to session properties on query results.
- Added host id to session network.
- Added better error message for a GUID collision edge case for Multiplay hosted sessions.

### Fixed
- Fixed first time Matchmaker queue file deployment that would leave the Matchmaker disabled if no Matchmaker Environment file was deployed along the queue files.
- Fixed the potential for an awaited successful session creation getting interrupted by an exception thrown from a registered event handler.
- Fixed the network connection attempting to parse an IP address as IPV6 on platforms where IPV6 is not supported.

## [1.1.3] - 2025-04-02

### Added
- Added session refresh after subscription to events.

### Fixed
- Fixed DGS not properly deleting the lobby when it leaves it or stops.
- Fixed inability to catch exception in the Matchmaker process if thrown after the match was found.
- Fixed lobby heartbeat start on host changed.
- Fixed `ArgumentNullException` being thrown when selecting specific `Deployment` files in the Project Window.
- Fixed inconsistent session changed events.

## [1.1.2] - 2025-03-03

### Fixed
- Calling `SubscribeToLobbyEventsAsync` multiple times on the same lobby no longer throws exception.

## [1.1.1] - 2025-01-31

### Added
- Added migration path validation to warn users when they are using incompatible packages namely the lobby, matchmaker, multiplay and relay standalone SDK.

### Changed
- Increased Lobby max player count to 150.
- Removing a player that is not in the lobby no longer throws a `LobbyServiceException`. Instead, if verbose logging is enabled, it will log that the player was not found.
- Updated error when trying to access `MultiplayService.Instance` while in the editor.
- Subscribing to lobby events when a player is already subscribed no longer throws a `LobbyServiceException`. Instead, if verbose logging is enabled, it will log that the player is already subscribed.

### Fixed
- Fixed lobby so it no longer throws `ArgumentException: An item with the same key has already been added`.
- Fixed unauthorized error when trying to remove other players from a lobby, with service account authentication.

## [1.1.0] - 2024-11-19

### Added
- Added more detail in `SessionException` message for `MatchmakerAssignmentFailed` and `MatchmakerAssignmentTimeout`, and exposed the Error property via `ToString()`.
- Added 2 new events under `ISession`, `ISession.PlayerLeaving` and `ISession.PlayerHasLeft`.

### Changed
- Marked the `ISession.PlayerLeft` event as obsolete. It is getting replaced by the new `ISession.PlayerLeaving` event.
- Increased timeout when uploading files from a build.

### Fixed
- Fixed Session backfilling configuration:
  - Fixed `WithBackfillingConfiguration` setting `backfillingLoopInterval` as the `playerConnectionTimeout`.
  - Added the missing `playerConnectionTimeout` parameter to `WithBackfillingConfiguration`.
- Fixed the `ISession` extension method `GetMatchmakingResults` when used with MatchId Matchmaking and non-backfill Multiplay Matchmaking.
- Deprecated the `WithBackfillingConfiguration` method and replaced it with the corrected method with the same name and the missing `playerConnectionTimeout` parameter.
- Fixed Lobby Vivox interoperability issues around joining certain channel types or joining channels that didn't match a Lobby ID when trying only to use the Vivox SDK while the Lobby SDK was present in the project.
- Fixed the Lobby Vivox channel validation to allow for positional 3D channels.
- Fixed the Server Query Protocol (SQP) responses from Multiplay Hosting servers to include correct Version and Port.
- Fixed potential issue when querying for fleet status in the Deployment Window.
- Fixed Help URL links.

## [1.0.2] - 2024-10-28

### Fixed
- Fixed WebGL support for Distributed Authority.

## [1.0.1] - 2024-10-21

### Fixed
- Fixed an issue preventing Multiplay config files proper reimport and deploy.

## [1.0.0] - 2024-09-18

### Added
- Added QoS region selection for Distributed Authority session creation if none is passed.
- Added the ability to query the session the player has joined with `IMultiplayerService.GetJoinedSessionIdsAsync`.
- Added the ability to reconnect to a session with `IMultiplayerService.ReconnectToSessionAsync`.
- Added the ability to exclude paths on a Game Server Hosting build that supports basic patterns (*, ?).
- Added validation when accessing the `IMultiplaySessionManager.Session`.
- Added `$schema` doc field to both Queue and Environment config files.
- Added documentation on `defaultQoSRegionName`.
- Added settings to game server hosting configuration schema:
  - Added server density settings (`usageSettings`) in `fleets`.

### Changed
- Updated `com.unity.services.wire` from 1.2.6 to 1.2.7 to fix reconnection issues notably with the lobby.
- Updated matchmaker deployment window:
  - Made `defaultQoSRegionName` a valid region: `North America`.
  - Ensured `backfillEnabled` is no longer ignored.
- Made the QoS Calculator class internal.
- Marked server hardware settings as deprecated in `buildConfigurations` in Game Server Hosting configuration schema.
- Updated documentation to replace Game Server Hosting with Multiplay Hosting.
- Updated minimum required version for Netcode for GameObjects from 2.0.0-pre.3 to 2.0.0.
- Updated minimum required version for Netcode for Entities from 1.3.0-pre.2 to 1.3.2.
- Changed connection metadata visibility to only be visible to members of the session.
- Updated Distributed Authority session properties.
- Enhanced exception messages on `ClientServerBootstrap` worlds checks.

### Fixed
- Fixed matchmaker deployment window:
  - Fixed deploying queue when the remote queue has filtered pools.
  - Fixed deploying queue when the remote queue has no pools.
- Fixed default value for session property constructor.
- Fixed `SessionHandler` dropping property's index when updating them.
- Fixed session cleanup when a player polls for session updates and is kicked from the session.
- Fixed session error on deleting a non-existing session.
- Fixed port randomization compatibility with Network for GameObjects.
- Fixed occasional failure to fetch matchmaking results from P2P matches:
  - Properly uploaded these results.
- Fixed matchmaking results 204 exception.
- Fixed broken links in Multiplay Hosting documentation.
- Fixed error relating to `ENABLE_UCS_SERVER` scripting define to support limited server functionality via Play Mode using a non-server build profile.
- Fixed `TaskCanceledException` when starting an SQP server in Game Server Hosting.
- Fixed `SavePropertiesAsync` not saving session fields if properties are unchanged.
- Fixed typo in `SessionError`.

## [1.0.0-pre.1] - 2024-07-18

### Added
- Added ability to update the session published port with `NetworkConfiguration.UpdatePublishPort` to enable auto-port selection in network handlers.
- Added **View in Deployment Window** button for Game Server Hosting and Matchmaker config-as-code resource files, dependent on Deployment package version 1.4.0.

### Changed
- Updated default values for direct network options:
  - `listenIp` and `publishIp` default to `127.0.0.1`.
  - `port` defaults to `0`.
- Updated network support in sessions for Netcode for Entities to version 1.3.0-pre.2.
- Updated network support in sessions for Netcode for GameObjects v2 to version 2.0.0-pre.1 (required for Distributed Authority).

### Fixed
- Fixed issue where Game Server Hosting deploy upload may fail in some cases.

## [0.6.0] - 2024-07-10

### Added
- Added Apple privacy manifest.
- Added missing List and Delete APIs for Build configuration and Builds.
- Added missing documentation.

### Changed
- Renamed session connection operations to network branding.
- Updated `com.unity.services.wire` dependency to 1.2.6.

### Fixed
- Fixed issue where the notification system would fail to reconnect silently.

## [0.5.0] - 2024-06-18

### Added
- Added session matchmaking support for peer-to-peer and dedicated game servers.
- Added Multiplay server lifecycle support & server session management.
- Added matchmaker backfilling support for server sessions.
- Added session authorization flow for distributed authority.
- Added session filters for session matchmaking and queries.
- Added automatic attempt to leave a session when leaving the application/play mode.
- Added session viewer editor window for better observability.
- Added matchmaker deployment support.

### Changed
- Made minor improvements to sessions.

## [0.4.2] - 2024-05-28

### Changed
- Updated documentation.

## [0.4.1] - 2024-05-17

### Changed
- Updated some name changes in Netcode for GameObjects v2.0.0-exp.3.

## [0.4.0] - 2024-04-23

### Changed
- Renamed package from Multiplayer Services SDK to Multiplayer Services.

## [0.3.0] - 2024-04-04

### Added
- Added support for Distributed Authority with Netcode for GameObjects 2.0.

### Changed
- Ensured deployment window integration compatibility with Multiplay package:
  - Multiplay owns the integration from [1-1.2).
  - Unified package owns it onwards.

## [0.2.0] - 2024-03-26

### Added
- Added session delete API.

### Changed
- Set player properties on join.
- Abstracted session host concept.
- Refactored `SessionInfo`.

### Removed
- Removed `PlayerProfile` from `ISession`.

### Fixed
- Fixed session to honour session data when creating lobby.

## [0.1.0] - 2024-03-11

### Added
- Initial Multiplayer SDK sessions implementation.
- Added common Multiplayer Backend behind a feature flag:
  - Standalone functions available and support for the matchmaking flow (matchmake into a CMB session).
- Added IP Address as an optional field in Multiplay ServerConfig.

### Removed
- Removed `PostBuildHook` and `EventConnectionStateChanged`.

## [0.0.7] - 2023-08-23

### Changed
- Updated documentation.

## [0.0.6] - 2023-08-21

### Changed
- Updated README.

## [0.0.5] - 2023-08-16

### Changed
- Updated the minimum supported Editor version to 2021.3.
- Updated README with links to consolidated SDK documentation.

## [0.0.4] - 2023-08-15

### Changed
- Updated `.npmignore`.

### Removed
- Removed samples from the package.

## [0.0.3] - 2023-08-14

### Changed
- Unexported `MatchHandlerImpl`.
- Made API changes.

## [0.0.2] - 2023-08-10

### Changed
- Updated README.

## [0.0.1] - 2023-08-09

### Added
- Initial SDK.
