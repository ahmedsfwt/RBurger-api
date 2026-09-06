using MediatR;

namespace RBurger.Application.Authentication.Commands.Logout;

// Day 14 addition (Backend Parity Spec §5). Not documented in v1.2 at all - no logout route,
// request shape, or auth requirement is specified anywhere in Documentation v1.2.
//
// CONTRACT DECISION (flagged, not silently invented): the smallest contract consistent with
// the existing, already-documented refresh flow is reused exactly - §7.1's
// POST /api/v1/auth/refresh identifies the token via a body field ({ "refreshToken": "..." }),
// so logout uses the identical shape and is likewise Public (no Authorize attribute): the
// refresh token itself is the credential that identifies which session to end, exactly as it
// already is for /refresh. No new header/cookie/query convention is introduced.
public record LogoutCommand(string RefreshToken) : IRequest;
