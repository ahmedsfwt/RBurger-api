namespace RBurger.Application.Authentication.DTOs;

// §7.1 POST /api/v1/auth/refresh response.
//
// Day 16 (Backend Parity Spec §3 - explicitly APPROVED contract change): now includes the
// replacement refreshToken. The original Documentation v1.2 example shows only
// { accessToken, expiresInSeconds } and Day 13/14/15 deliberately withheld this field for
// that reason - Ahmed has now explicitly approved adding it ("Refresh endpoint MUST return
// the replacement refreshToken... this is now APPROVED... this is the explicitly APPROVED
// refresh-token response change"), which resolves the rotation-without-a-usable-successor
// gap flagged in every prior day's report. This is the ONE deliberate, approved deviation
// from the literal §7.1 example payload.
public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

