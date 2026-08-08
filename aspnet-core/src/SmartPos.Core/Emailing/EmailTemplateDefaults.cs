namespace SmartPos.Emailing
{
    public static class EmailTemplateDefaults
    {
        public static string EmailLoginCodeBodyHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>Sign-in code</title>
</head>
<body style=""margin:0;padding:0;background:#f4f6f8;font-family:Segoe UI,Arial,sans-serif;color:#1f2937;"">
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background:#f4f6f8;padding:32px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""max-width:560px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 8px 24px rgba(15,23,42,0.08);"">
          <tr>
            <td style=""background:#0f766e;padding:24px 28px;color:#ffffff;"">
              <div style=""font-size:20px;font-weight:700;letter-spacing:0.02em;"">{{AppName}}</div>
              <div style=""margin-top:6px;font-size:14px;opacity:0.9;"">Email sign-in code</div>
            </td>
          </tr>
          <tr>
            <td style=""padding:28px;"">
              <p style=""margin:0 0 12px;font-size:16px;"">Hi {{Name}},</p>
              <p style=""margin:0 0 20px;font-size:15px;line-height:1.5;color:#4b5563;"">
                Use this one-time code to sign in to your account. It expires in <strong>{{ExpirationMinutes}} minutes</strong>.
              </p>
              <div style=""text-align:center;margin:28px 0;"">
                <div style=""display:inline-block;padding:14px 28px;border-radius:10px;background:#ecfdf5;border:1px solid #99f6e4;font-size:32px;font-weight:700;letter-spacing:0.35em;color:#0f766e;"">
                  {{Code}}
                </div>
              </div>
              <p style=""margin:0 0 8px;font-size:13px;color:#6b7280;line-height:1.5;"">
                If you did not request this code, you can ignore this email. Your account will stay secure.
              </p>
              <p style=""margin:16px 0 0;font-size:12px;color:#9ca3af;"">
                Sent to {{Email}} ({{UserName}})
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        public static string BranchActivationBodyHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>Activate branch</title>
</head>
<body style=""margin:0;padding:0;background:#f4f6f8;font-family:Segoe UI,Arial,sans-serif;color:#1f2937;"">
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background:#f4f6f8;padding:32px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""max-width:560px;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 8px 24px rgba(15,23,42,0.08);"">
          <tr>
            <td style=""background:#0f766e;padding:24px 28px;color:#ffffff;"">
              <div style=""font-size:20px;font-weight:700;letter-spacing:0.02em;"">{{AppName}}</div>
              <div style=""margin-top:6px;font-size:14px;opacity:0.9;"">Branch activation</div>
            </td>
          </tr>
          <tr>
            <td style=""padding:28px;"">
              <p style=""margin:0 0 12px;font-size:16px;"">Hello,</p>
              <p style=""margin:0 0 20px;font-size:15px;line-height:1.5;color:#4b5563;"">
                The branch <strong>{{BranchName}}</strong> for tenant <strong>{{TenantName}}</strong> has been approved.
                Open the link below to activate it. The link expires in <strong>{{ExpirationHours}} hours</strong>.
              </p>
              <div style=""text-align:center;margin:28px 0;"">
                <a href=""{{ActivationLink}}"" style=""display:inline-block;padding:14px 28px;border-radius:10px;background:#0f766e;color:#ffffff;font-size:15px;font-weight:700;text-decoration:none;"">
                  Activate branch
                </a>
              </div>
              <p style=""margin:0 0 8px;font-size:13px;color:#6b7280;line-height:1.5;"">
                If the button does not work, copy and paste this link into your browser:
              </p>
              <p style=""margin:0;font-size:12px;color:#0f766e;word-break:break-all;line-height:1.5;"">
                {{ActivationLink}}
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }
    }
}
