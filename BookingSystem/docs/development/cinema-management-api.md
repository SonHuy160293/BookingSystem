# Cinema management API

## Endpoints

All routes are version 1 management routes. Responses use the shared `{ success, data, error }` envelope.

| Resource | Paging | Detail | Create | Replace editable fields |
| --- | --- | --- | --- | --- |
| Cinema | GET /api/v1/Cinemas | GET /api/v1/Cinemas/{id} | POST /api/v1/Cinemas | PUT /api/v1/Cinemas/{id} |
| Film | GET /api/v1/Films | GET /api/v1/Films/{id} | POST /api/v1/Films | PUT /api/v1/Films/{id} |
| ScreeningRoom | GET /api/v1/ScreeningRooms | GET /api/v1/ScreeningRooms/{id} | POST /api/v1/ScreeningRooms | PUT /api/v1/ScreeningRooms/{id} |

POST returns 201 and a versioned Location. PUT returns 200. Write responses are read after the command transaction commits so audit timestamps reflect saved state. For Cinema and Film, that internal read uses the write action's authorization; a creator need not also have View permission to receive their response. Following a Cinema or Film Location independently requires View.

PUT is replacement of editable fields: omitted optional text/date values become null, and empty arrays clear Film metadata or genres. Required fields must be present, including Cinema availability flags and all Film arrays. IDs and tenant ownership cannot be changed.

## Authorization

Cinema and Film actions validate JWTs with the existing authentication configuration. Each has the matching Cinemas.View/Create/Update or Films.View/Create/Update permission filter. Their Application handlers additionally consult current Identity role assignments; permission claims alone do not establish scope.

ScreeningRoom access checks are deferred. Its four endpoints have no controller authorization attributes or permission filters and no Application access checks. Under the current configuration they allow anonymous access and query all matching rooms without tenant/branch scoping. They do not call Identity for authorization.

- Film management requires a platform-scoped grant.
- Tenant-scoped Cinema grants cover that tenant's venues.
- Branch-scoped Cinema grants cover only the corresponding venue.
- An explicitly granted platform Cinema permission covers all venues.
- Disabled users, locked-out users and disabled permission definitions do not grant access.
- Cinema resource predicates apply before Count, sorting and pagination. TenantId filters only narrow access.
- Identity results are cached only within the current HTTP request, separately for each permission.
- An unavailable Identity service fails the operation; it never supplies permissive access.

Authorization is evaluated at request time. There is no distributed transaction with Identity; grant revocation during an already-authorized request is not atomic with the Cinema write.

## Branch ownership and integration

CreateCinemaRequest contains BranchId, RegionId, Name, Slug, Address, optional Description/Latitude/Longitude, and explicit PublicationStatus, OperatingStatus and OnlineBookingEnabled. It contains no TenantId.

Create the administrative Branch in Identity first. Cinema resolves the existing Branch, authorizes its owner/scope, then uses Branch.Id as Cinema.Id and the verified Branch.TenantId as Cinema.TenantId. It does not create a Branch or emit provisioning events.

Status values use the existing numeric enums: PublicationStatus is Draft=0, Published=1, Archived=2; OperatingStatus is Open=0, OpeningSoon=1, Closed=2.

Identity exposes service-key-protected integration endpoints:

- GET /api/v1/integrations/CinemaAccess/users/{userId}?permission=Cinemas.View
- GET /api/v1/integrations/CinemaAccess/branches/{id}

The permission parameter accepts only the six Cinema/Film permission values above. The first response's data contains HasPlatformAccess, TenantIds, and Branches (Id/TenantId pairs); the second contains Id/TenantId. JSON uses the normal web naming policy.

The integration key authenticates the calling service. Cinema obtains userId from the authenticated principal, never from public request input. Treat the key as a trusted service credential. Both modules own their small wire DTOs; neither references the other's projects or tables.

## Configuration and rollout

Supply these through the existing environment/secrets mechanism. No credentials are committed by this feature.

| Host | Settings |
| --- | --- |
| Cinema API | ConnectionStrings__ApplicationDb: existing PostgreSQL connection |
| Cinema API | Jwt__SecretKey (at least 32 characters), Jwt__Issuer, Jwt__Audience, Jwt__AccessTokenExpirationMinutes |
| Cinema API | IdentityIntegration__BaseUrl: reachable Identity service root URL, including any path prefix and using HTTPS in deployed environments |
| Cinema API | IdentityIntegration__ApiKey: configured service key |
| Cinema API | IdentityIntegration__HeaderName: optional; defaults to X-Integration-Key |
| Identity API | IntegrationApi__ApiKeys__0: matching service key |
| Identity API | IntegrationApi__HeaderName: same header name; defaults to X-Integration-Key |

The Cinema API validates required configuration on startup. MigrationRunner registers persistence without the Identity HTTP client and does not need these API credentials.

Use an existing trusted token issuer with subject/user identifier and Permission claims. Identity's AuthController currently does not implement token issuance; this feature does not add login.

Run the existing permission-definition synchronization through Identity's established deployment workflow, and assign the new grants to appropriate roles. Cinema permissions have Branch as their minimum scope, permitting tenant/platform roles under existing Identity assignment rules; Film permissions require Platform.

The mapped Cinema tables, Region/Genre reference data and Identity Branches must exist. This feature does not generate/apply migrations or seed business reference data.

## Requests and paging

Film create/update fields: Title, EnglishTitle, Synopsis, EnglishSynopsis, DurationMinutes, ReleaseDate (ISO calendar date), AgeClassificationCode, PosterUrl, BannerUrl, TrailerUrl, TrailerVideoUrl, CountryCodes, FormatTags, CaptionOptions, GenreIds.

GenreIds resolve existing local lookup rows; duplicates are consolidated and unknown IDs return 400. Film titles are not unique. Metadata lists retain domain normalization and JSONB storage. Country code validation checks two ASCII letters, not membership in an external country registry.

Paging uses PageNumber (default 1), PageSize (default 20, capped at 100), Search, SortBy and SortDirection. Existing normalization handles nonpositive page/size values. Unknown sorting or invalid filters returns 400; out-of-range pages return empty items with the filtered total.

| Resource | Search | Filters | SortBy |
| --- | --- | --- | --- |
| Cinema | Name, Slug, Address | TenantId, RegionId, PublicationStatus, OperatingStatus, OnlineBookingEnabled | Name (default), CreatedAt, Id |
| Film | Title, EnglishTitle | GenreId, ReleaseDateFrom, ReleaseDateTo, AgeClassificationCode | Title (default), ReleaseDate, DurationMinutes, CreatedAt, Id |
| ScreeningRoom | Code, Name | CinemaId, IsActive | Code (default), Name, CreatedAt, Id |

Search is case-insensitive; percent/underscore characters are literal search text. Id breaks sorting ties. Null release dates sort last in either direction.

## Persistence and errors

EF Core handles reads and writes. DTO queries do not track and paging occurs in SQL. Film detail queries address JSONB backing fields explicitly. Film updates track existing memberships, change only the Film aggregate/memberships, and do not modify Genre lookup records.

The existing command transaction pipeline owns SaveChanges. Cinema's unit of work translates known Cinema primary-key/slug violations and ScreeningRoom code uniqueness violations into 409; concurrent removal of a selected Region, Genre or parent Cinema becomes 400. Other database failures retain existing middleware handling.

Expected responses: 400 invalid input/reference, 401 unauthenticated, 403 insufficient permission, 404 missing or inaccessible resource, 409 duplicate Cinema profile/slug. Upstream service failures use the existing generic error handling; they are not mistaken for user permission denial.

Updates use the existing last-writer behavior; this API does not introduce an optimistic concurrency token.

## ScreeningRoom requests and acceptance scenarios

Create requires CinemaId, Code and Name. The Cinema must already exist locally; its ID also identifies the Identity Branch. Creation starts the room active. Update requires Code, Name and an explicit IsActive boolean; it preserves CinemaId. Code is trimmed and uppercased, Name is trimmed, and their maximum lengths are 32 and 100 characters respectively.

Detail and write responses contain Id, CinemaId, Code, Name, IsActive, CreatedAt and UpdatedAt. Paging items omit UpdatedAt. Responses exclude parent Cinema details, seats, layouts and showtimes. Both reads use EF Core DTO projections without tracking; updates load the room through the tracked repository lookup and change it through domain methods.

Search is limited to 500 characters. Sort fields and asc/desc are case-insensitive. CinemaId and IsActive are optional filters; an explicitly empty CinemaId is invalid, while a nonexistent CinemaId produces an empty page. Code uniqueness is scoped to CinemaId and checked after normalization, excluding the current room on update. The unique index remains authoritative for concurrent writes.

The mapped ScreeningRooms table, UX_ScreeningRooms_CinemaId_Code index and FK_ScreeningRooms_Cinemas_CinemaId constraint must be deployed before these APIs can operate. Existing lowercase tables require separate rename migrations. This API change creates or applies no migrations.

Expected acceptance scenarios, documented without running tests or a verification loop:

- Create returns 201, an active room with normalized Code, saved audit values and a versioned Location resolving to its detail route.
- Duplicate normalized codes within one Cinema return 409 with ScreeningRoom.CodeExists; another Cinema can reuse the code.
- Update can retain its own code, replace name/code and toggle IsActive while preserving CinemaId.
- Missing Cinema on create returns 404 with Cinema.NotFound; missing room on detail/update returns 404 with ScreeningRoom.NotFound.
- Missing required fields, empty IDs, blank or overlong text, unsupported sort fields/directions and overlong search return 400. An explicit false IsActive value is valid.
- Paging applies filters/search before counts and pagination, treats percent/underscore/backslash as literal search text, and uses Id to break sort ties.
- Nonpositive page/size values use existing defaults; large sizes cap at 100 and out-of-range pages return empty items with the filtered total.
- Concurrent duplicate-code violations return the same 409 conflict; a parent Cinema deleted between validation and commit produces 400.
- All four ScreeningRoom endpoints work without login or permission checks under the current API configuration.
