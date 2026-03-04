# Migration to AKS - Changes Applied

**Date:** March 4, 2026

## Overview

This document summarizes all changes made to enable the IframeFullServer application to work seamlessly in **both** local Windows development and Azure Kubernetes Service (AKS) production environments.

---

## ✅ Changes Completed

### 1. **Configuration Files Updated**

Added `AppBaseUrl` configuration to all environment-specific settings files:

#### **appsettings.json** (Local Development)
```json
"AppBaseUrl": "http://localhost:44383"
```

#### **appsettings.LOCAL-EXAMPLE.json** (Local Template)
```json
"AppBaseUrl": "http://localhost:44383"
```

#### **appsettings.AKS-EXAMPLE.json** (AKS Template)
```json
"AppBaseUrl": "https://embed.yourdomain.com"
```

**Purpose:** Provides explicit base URL for each environment, eliminating hardcoded localhost references.

---

### 2. **Program.cs Enhanced**

#### **a) Added Using Statement**
```csharp
using Microsoft.AspNetCore.HttpOverrides;
```

#### **b) Added Forwarded Headers Middleware**
```csharp
// Configure forwarded headers for reverse proxy/ingress (AKS)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

**Purpose:** Enables the app to correctly interpret X-Forwarded-* headers from AKS ingress/load balancer, ensuring proper HTTPS URL generation.

#### **c) Updated Cookie Authentication**
```csharp
.AddCookie(options =>
{
    options.Cookie.Name = "UserCookie";
    options.LoginPath = "/Home/Login";
    // For cross-origin iframe scenarios (AKS deployment)
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

**Purpose:** Allows cookies to work in cross-origin iframe scenarios (required when Bold BI and this app are on different domains in AKS).

#### **d) Registered IHttpContextAccessor**
```csharp
services.AddHttpContextAccessor(); // For dynamic URL generation
```

**Purpose:** Enables dependency injection of `IHttpContextAccessor` for accessing request context in controllers.

---

### 3. **HomeController.cs Updated**

#### **a) Added Dependency Injection**
```csharp
private readonly IHttpContextAccessor _httpContextAccessor;

public HomeController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
{
    _configuration = configuration;
    _httpContextAccessor = httpContextAccessor;
}
```

#### **b) Updated Login Method**
**Before:**
```csharp
string redirectUrl = "http://localhost:44383/Home/Embed";
```

**After:**
```csharp
var baseUrl = GetBaseUrl();
string redirectUrl = $"{baseUrl}/Home/Embed";
```

#### **c) Updated Logout Method**
**Before:**
```csharp
var url = _configuration["jwt:boldbiserverurl"].TrimEnd('/')
    + "/oauth/logout?redirect_uri=http://localhost:44383/Home/Loginpage";
```

**After:**
```csharp
var baseUrl = GetBaseUrl();
var url = _configuration["jwt:boldbiserverurl"].TrimEnd('/') 
    + $"/oauth/logout?redirect_uri={baseUrl}/Home/Loginpage";
```

#### **d) Added GetBaseUrl Helper Method**
```csharp
/// <summary>
/// Gets the base URL for this application.
/// Tries configuration first (for explicit local/AKS settings), then falls back to auto-detection.
/// </summary>
private string GetBaseUrl()
{
    // Try to get from configuration first (works for both local and AKS)
    var configuredBaseUrl = _configuration["AppBaseUrl"];
    if (!string.IsNullOrEmpty(configuredBaseUrl))
    {
        return configuredBaseUrl.TrimEnd('/');
    }

    // Fallback: auto-detect from current request
    var request = _httpContextAccessor.HttpContext?.Request;
    if (request != null)
    {
        return $"{request.Scheme}://{request.Host}";
    }

    // Last resort fallback
    return "http://localhost:44383";
}
```

**Purpose:** Provides intelligent URL generation that:
1. First checks configuration (explicit for each environment)
2. Falls back to auto-detection from HTTP request
3. Has a safe default for edge cases

---

### 4. **Views/Home/Embed.cshtml Updated**

#### **Iframe Source URL**
**Before:**
```cshtml
<iframe src='http://localhost:44383/Home/JWTLogin' width='150%' height='1300px'></iframe>
```

**After:**
```cshtml
<iframe src='@Url.Action("JWTLogin", "Home", null, Context.Request.Scheme)' width='150%' height='1300px'></iframe>
```

#### **Login Link URL**
**Before:**
```cshtml
<a asp-controller="Home" asp-action="Loginpage" asp-route-returnURL="http://localhost:44383/Home/Embed">login</a>
```

**After:**
```cshtml
<a asp-controller="Home" asp-action="Loginpage" asp-route-returnURL="@Url.Action("Embed", "Home", null, Context.Request.Scheme)">login</a>
```

**Purpose:** Uses ASP.NET Core's built-in URL helpers to generate correct URLs based on the current request context.

---

### 5. **k8s/configmap.yaml Updated**

Added AppBaseUrl to Kubernetes ConfigMap:

```yaml
data:
  AppBaseUrl: "https://embed.yourdomain.com"
  jwt__boldbiserverurl: "https://yokogawa-testing-upgrade-site.boldbidemo.com"
  jwt__siteidentifier: "reports1"
  jwt__redirectto: "https://yokogawa-testing-upgrade-site.boldbidemo.com/bi/site/reports1"
  user__userid: "1"
  ExpirationTimeInMinutes: "120"
```

**Note:** Remember to replace `https://embed.yourdomain.com` with your actual AKS domain.

---

## 🎯 How It Works Now

### **Local Windows Development**

1. **appsettings.json** contains:
   ```json
   "AppBaseUrl": "http://localhost:44383"
   ```

2. Application uses this for all URL generation
3. Works with Bold BI on `http://localhost:65212`
4. No code changes needed - just configuration

### **AKS Production Deployment**

1. **k8s/configmap.yaml** contains:
   ```yaml
   AppBaseUrl: "https://embed.yourdomain.com"
   ```

2. Environment variable `AppBaseUrl` is read by the application
3. All URLs are generated with the production domain
4. Works with Bold BI on AKS with domain
5. Forwarded headers ensure HTTPS is correctly detected
6. Cookies work across different domains

---

## 🔧 Configuration Requirements

### **For Local Development**

**appsettings.json:**
```json
{
  "AppBaseUrl": "http://localhost:44383",
  "jwt:signingkey": "<YOUR-LOCAL-BOLD-SIGNING-KEY>",
  "user:userid": "1",
  "jwt:boldbiserverurl": "http://localhost:65212",
  "jwt:siteidentifier": "site1",
  "jwt:redirectto": "http://localhost:65212/bi/site/site1",
  "ExpirationTimeInMinutes": "120"
}
```

**Bold BI UMS Configuration:**
- Remote Login URL: `http://localhost:44383/Home/JWTLogin`
- Remote Logout URL: `http://localhost:44383/Home/JWTLogout`

---

### **For AKS Deployment**

**k8s/configmap.yaml:**
```yaml
data:
  AppBaseUrl: "https://embed.yourdomain.com"  # ← YOUR DOMAIN
  jwt__boldbiserverurl: "https://your-boldbi.com"  # ← YOUR BOLD BI URL
  jwt__siteidentifier: "prod-site"  # ← YOUR SITE ID
  jwt__redirectto: "https://your-boldbi.com/bi/site/prod-site"
  user__userid: "1"
  ExpirationTimeInMinutes: "120"
```

**k8s/secret.yaml:**
```yaml
stringData:
  jwt__signingkey: "<YOUR-AKS-BOLD-SIGNING-KEY>"  # ← FROM BOLD BI
```

**k8s/ingress.yaml:**
```yaml
spec:
  tls:
    - hosts:
        - embed.yourdomain.com  # ← YOUR DOMAIN
      secretName: iframefullserver-tls
  rules:
    - host: embed.yourdomain.com  # ← YOUR DOMAIN
```

**Bold BI UMS Configuration:**
- Remote Login URL: `https://embed.yourdomain.com/Home/JWTLogin`
- Remote Logout URL: `https://embed.yourdomain.com/Home/JWTLogout`
- Embed Settings: Add `https://embed.yourdomain.com` to allowed origins

---

## 🚀 Deployment Steps

### **Local Testing**

1. Update `appsettings.json` with local values
2. Run: `dotnet run --launch-profile http`
3. Open: `http://localhost:44383/Home/Embed`
4. ✅ Should work with local Bold BI

### **AKS Deployment**

1. **Update k8s manifests** with your values:
   - `k8s/configmap.yaml` - Set `AppBaseUrl` to your domain
   - `k8s/secret.yaml` - Set JWT signing key
   - `k8s/ingress.yaml` - Set your domain
   - `k8s/deployment.yaml` - Set ACR image path

2. **Build and push Docker image:**
   ```powershell
   az acr login --name <your-acr>
   docker build -t <your-acr>.azurecr.io/iframefullserver:1.0.0 .
   docker push <your-acr>.azurecr.io/iframefullserver:1.0.0
   ```

3. **Deploy to AKS:**
   ```powershell
   kubectl apply -f .\k8s\namespace.yaml
   kubectl apply -f .\k8s\configmap.yaml
   kubectl apply -f .\k8s\secret.yaml
   kubectl apply -f .\k8s\deployment.yaml
   kubectl apply -f .\k8s\service.yaml
   kubectl apply -f .\k8s\ingress.yaml
   ```

4. **Configure DNS:**
   - Get ingress IP: `kubectl get ingress -n boldbi-embed`
   - Create A record: `embed.yourdomain.com` → `<INGRESS-IP>`

5. **Update Bold BI UMS** with your app's URLs

6. **Test:** Open `https://embed.yourdomain.com/Home/Embed`

---

## 🔍 Key Benefits

✅ **Single Codebase** - No code changes between environments  
✅ **Configuration-Based** - Each environment has its own config  
✅ **Auto-Detection** - Falls back to request-based URL generation  
✅ **Cross-Origin Support** - Cookies work across different domains  
✅ **HTTPS Ready** - Properly handles reverse proxy headers  
✅ **Maintainable** - Clear separation of environment concerns  

---

## ⚠️ Important Notes

1. **AppBaseUrl must match your actual domain** in each environment
2. **JWT signing key must match** between this app and Bold BI UMS
3. **HTTPS is required** for AKS deployment (cookies won't work without it)
4. **Bold BI Embed Settings** must include your domain
5. **DNS must point** to AKS ingress before testing

---

## 🐛 Troubleshooting

### **Issue: URLs still show localhost in AKS**

**Check:**
1. `k8s/configmap.yaml` has correct `AppBaseUrl`
2. ConfigMap was applied: `kubectl get configmap iframefullserver-config -n boldbi-embed -o yaml`
3. Pod was restarted after ConfigMap update: `kubectl rollout restart deployment iframefullserver -n boldbi-embed`

### **Issue: Cookies not working**

**Check:**
1. Both apps are using HTTPS
2. Bold BI Embed Settings includes your domain
3. Browser DevTools → Application → Cookies (verify `SameSite=None; Secure`)

### **Issue: Incorrect redirect URLs**

**Check:**
1. `AppBaseUrl` in config doesn't have trailing slash
2. ForwardedHeaders middleware is before UseHttpsRedirection
3. Ingress controller is sending proper X-Forwarded-Proto headers

---

## 📝 Summary

All hardcoded `localhost:44383` references have been replaced with dynamic URL generation that works in both local and AKS environments. The application now:

- Reads `AppBaseUrl` from configuration
- Falls back to auto-detection if not configured
- Handles forwarded headers from AKS ingress
- Supports cross-origin cookies for iframe embedding
- Works seamlessly in both Windows and Kubernetes

**No further code changes are needed** - only environment-specific configuration.

---

## 📚 Related Files Modified

- ✅ `appsettings.json`
- ✅ `appsettings.LOCAL-EXAMPLE.json`
- ✅ `appsettings.AKS-EXAMPLE.json`
- ✅ `Program.cs`
- ✅ `Controllers/HomeController.cs`
- ✅ `Views/Home/Embed.cshtml`
- ✅ `k8s/configmap.yaml`

---

**Ready for deployment!** 🚀
