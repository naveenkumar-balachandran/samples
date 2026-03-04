# IframeFullServer - Bold Reports JWT Authentication Sample

ASP.NET Core 8.0 sample application demonstrating JWT-based authentication with Bold Reports for iframe embedding.

## 🚀 Quick Start

### Option 1: Local Testing (5 minutes)

1. **Copy example config:**
   ```powershell
   Copy-Item appsettings.LOCAL-EXAMPLE.json appsettings.json
   ```

2. **Get Bold UMS Signing Key:**
   - Open Bold UMS: `http://localhost:65212/ums/administration/sso`
   - Go to Authentication → JWT SSO
   - Copy the **Signing Key**

3. **Update appsettings.json:**
   ```json
   {
     "jwt": {
       "signingkey": "<PASTE-KEY-HERE>",
       "boldbiserverurl": "http://localhost:65212",
       "siteidentifier": "site1",
       "redirectto": "http://localhost:65212/bi/site/site1"
     }
   }
   ```

4. **Configure Bold UMS:**
   - Remote Login URL: `http://localhost:44383/Home/JWTLogin`
   - Remote Logout URL: `http://localhost:44383/Home/JWTLogout`
   - Signing Key: Same as step 2

5. **Run:**
   ```powershell
   dotnet run --launch-profile http
   ```

6. **Test:** Open `http://localhost:44383/Home/Embed`

### Option 2: Test with AKS Bold Reports (10 minutes)

1. **Install cloudflared:**
   ```powershell
   choco install cloudflared
   ```

2. **Copy AKS example config:**
   ```powershell
   Copy-Item appsettings.AKS-EXAMPLE.json appsettings.json
   ```

3. **Update appsettings.json** with your AKS Bold Reports URL and signing key

4. **Start app and tunnel:**
   ```powershell
   # Terminal 1
   dotnet run --launch-profile http
   
   # Terminal 2
   cloudflared tunnel --url http://localhost:44383
   ```

5. **Configure AKS Bold UMS** with your tunnel URL (e.g., `https://xyz.trycloudflare.com/Home/JWTLogin`)

6. **Test:** Open the tunnel URL

### Option 3: Deploy to AKS (30 minutes)

See [DEPLOYMENT-GUIDE.md](DEPLOYMENT-GUIDE.md) for complete instructions.

---

## 📁 Files Overview

| File | Purpose |
|------|---------|
| `Program.cs` | App configuration, middleware, services |
| `Controllers/HomeController.cs` | Login, logout, JWT generation logic |
| `Models/TokenHelper.cs` | JWT token generation |
| `Views/Home/Embed.cshtml` | Iframe embedding page |
| `appsettings.json` | Configuration (use examples to populate) |
| `Dockerfile` | Container image definition |
| `k8s/*.yaml` | Kubernetes deployment manifests |

---

## 🔧 Configuration

### Required Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `jwt:signingkey` | Must match Bold UMS JWT signing key | Base64 string |
| `jwt:boldbiserverurl` | Bold Reports base URL | `https://reports.example.com` |
| `jwt:siteidentifier` | Bold site identifier | `site1` |
| `jwt:redirectto` | Where to navigate after JWT login | `https://reports.example.com/bi/site/site1` |

### Environment Variables (AKS)

In Kubernetes, use double underscores:
- `jwt:signingkey` → `jwt__signingkey`
- `jwt:boldbiserverurl` → `jwt__boldbiserverurl`

See `k8s/configmap.yaml` and `k8s/secret.yaml`.

---

## 🐛 Troubleshooting

### Blank iframe
- Check Browser DevTools Console for errors
- Verify `jwt:boldbiserverurl` is correct
- Check Network tab for failed POST to Bold UMS

### 400 Bad Request
- **Most common:** Signing key mismatch
- Verify Bold UMS and appsettings.json have identical keys
- Check site identifier exists in Bold UMS
- Ensure system time is correct

### Cookie issues
- Use HTTPS (required for `SameSite=None`)
- Verify both apps are on HTTPS
- Check Bold UMS allows your domain in embed settings

### localhost URLs in production
- Verify `UseForwardedHeaders` is in `Program.cs`
- Check it's placed BEFORE `UseHttpsRedirection`

---

## 📚 Documentation

- [DEPLOYMENT-GUIDE.md](DEPLOYMENT-GUIDE.md) - Complete deployment instructions
- [Bold BI JWT Documentation](https://help.boldbi.com/security-configuration/single-sign-on/json-web-token/)

---

## 🏗️ Architecture

```
User Browser
    │
    ├─→ Sample App (localhost or AKS)
    │   ├─ Login form
    │   ├─ Generate JWT token
    │   └─ POST to Bold UMS
    │
    └─→ Bold Reports (localhost or AKS)
        ├─ Validate JWT
        ├─ Create session
        └─ Render reports in iframe
```

---

## ✅ Checklist: Before Sharing with DevOps

- [ ] Test works locally
- [ ] Test works with Cloudflare tunnel (if using AKS Bold)
- [ ] `appsettings.json` has empty/placeholder values (no secrets)
- [ ] `k8s/configmap.yaml` and `k8s/secret.yaml` have placeholders
- [ ] `k8s/deployment.yaml` has correct ACR image path
- [ ] `k8s/ingress.yaml` has correct hostname
- [ ] DEPLOYMENT-GUIDE.md is reviewed and accurate

---

## 🔐 Security Notes

1. **Never commit real signing keys** to source control
2. Use Azure Key Vault for production secrets
3. Restrict `KnownProxies` in `Program.cs` for production
4. Enable Kubernetes Secrets encryption at rest
5. Use managed identities where possible

---

## 📦 What to Share with DevOps

Provide these files:
- All source code (Controllers, Models, Views, Program.cs, etc.)
- `Dockerfile` and `.dockerignore`
- `k8s/*.yaml` manifests
- `DEPLOYMENT-GUIDE.md`
- This `README.md`

DevOps will:
1. Build Docker image
2. Push to ACR
3. Update manifests with environment-specific values
4. Deploy to AKS
5. Configure DNS and TLS
6. Update Bold UMS settings

---

## 🤝 Support

For issues or questions:
1. Check [DEPLOYMENT-GUIDE.md](DEPLOYMENT-GUIDE.md) troubleshooting section
2. Review Bold BI logs
3. Check application logs (`kubectl logs` for AKS)
4. Verify DevTools Network tab for HTTP errors
