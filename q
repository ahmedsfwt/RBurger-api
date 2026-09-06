[33mcommit 29ff0e89b4099a928407925df6c788d30f0af09f[m
Author: ahmedsfwt <ahmedsfwt22@gmail.com>
Date:   Sun Sep 6 03:10:41 2026 +0300

    Initial commit: RBurger backend - Clean Architecture, full API v1.2 parity

[1mdiff --git a/src/RBurger.Api/appsettings.json b/src/RBurger.Api/appsettings.json[m
[1mnew file mode 100644[m
[1mindex 0000000..16daa0a[m
[1m--- /dev/null[m
[1m+++ b/src/RBurger.Api/appsettings.json[m
[36m@@ -0,0 +1,27 @@[m
[32m+[m[32m{[m
[32m+[m[32m  "Logging": {[m
[32m+[m[32m    "LogLevel": {[m
[32m+[m[32m      "Default": "Information",[m
[32m+[m[32m      "Microsoft.AspNetCore": "Warning"[m
[32m+[m[32m    }[m
[32m+[m[32m  },[m
[32m+[m[32m  "AllowedHosts": "*",[m
[32m+[m[32m  "ConnectionStrings": {[m
[32m+[m[32m    "DefaultConnection": ""[m
[32m+[m[32m  },[m
[32m+[m[32m  "Jwt": {[m
[32m+[m[32m    "Issuer": "",[m
[32m+[m[32m    "Audience": "",[m
[32m+[m[32m    "Key": "",[m
[32m+[m[32m    "ExpiresInSeconds": 3600[m
[32m+[m[32m  },[m
[32m+[m[32m  "RefreshToken": {[m
[32m+[m[32m    "ExpiresInDays": 30[m
[32m+[m[32m  },[m
[32m+[m[32m  "RateLimiting": {[m
[32m+[m[32m    "AuthPermitLimit": 10,[m
[32m+[m[32m    "AuthWindowSeconds": 60,[m
[32m+[m[32m    "PaymentPermitLimit": 20,[m
[32m+[m[32m    "PaymentWindowSeconds": 60[m
[32m+[m[32m  }[m
[32m+[m[32m}[m
