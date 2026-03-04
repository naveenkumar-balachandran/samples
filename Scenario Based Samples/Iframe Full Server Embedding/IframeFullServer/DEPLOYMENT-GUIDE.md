# IframeFullServer Deployment Guide

Complete guide for deploying the JWT authentication sample in both Windows (local/tunnel) and Azure Kubernetes Service (AKS) environments.

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Configuration Overview](#configuration-overview)
3. [Local Windows Testing](#local-windows-testing)
4. [Cloudflare Tunnel Testing](#cloudflare-tunnel-testing)
5. [AKS Deployment](#aks-deployment)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### For Local Testing
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Bold Reports running (local or remote)

### For Cloudflare Tunnel Testing
- Cloudflare account (free tier works)
- cloudflared CLI installed

### For AKS Deployment
- Azure subscription
- Azure CLI installed
- kubectl installed
- Docker Desktop (for building images)
- Azure Container Registry (ACR)
- AKS cluster with NGINX Ingress Controller

---

## Configuration Overview

The app reads configuration from **appsettings.json** (local) or **environment variables** (containers/AKS).

### Configuration Keys

| Key | Description | Example (Local) | Example (AKS) |
|-----|-------------|----------------|---------------|
| `jwt:signingkey` | JWT signing key (must match Bold UMS) | `<your-key>` | From Bold UMS JWT settings |
| `jwt:boldbiserverurl` | Bold Reports base URL | `http://localhost:65212` | `https://reports.example.com` |
| `jwt:siteidentifier` | Bold site identifier | `site1` | `prod-site` |
| `jwt:redirectto` | Where to redirect after login | `http://localhost:65212/bi/site/site1` | `https://reports.example.com/bi/site/prod-site` |
| `user:userid` | Default user ID for JWT | `1` | `1` |
| `ExpirationTimeInMinutes` | Token validity | `120` | `120` |

### Environment Variable Mapping (AKS)

ASP.NET Core uses double underscores (`__`) to map hierarchical config:

```
jwt:signingkey       → jwt__signingkey
jwt:boldbiserverurl  → jwt__boldbiserverurl
jwt:siteidentifier   → jwt__siteidentifier
jwt:redirectto       → jwt__redirectto
user:userid          → user__userid
```

---

## Local Windows Testing

### Step 1: Configure Bold Reports (Local)

If testing with a local Bold Reports instance:

1. Open Bold UMS: `http://localhost:<port>/ums/administration/sso`
2. Navigate to **Authentication → JWT SSO**
3. Configure:
   - **Enable JWT SSO**: ✅ Checked
   - **Remote Login URL**: `http://localhost:44383/Home/JWTLogin`
   - **Remote Logout URL**: `http://localhost:44383/Home/JWTLogout`
   - **Signing Key**: Generate or copy existing key
   - **Enable Encryption**: ❌ Unchecked (unless using RSA)
4. Click **Save**

### Step 2: Update appsettings.json

Edit `appsettings.json`:

```json
{
  "jwt:signingkey": "<PASTE-SIGNING-KEY-FROM-BOLD-UMS>",
  "user:userid": "1",
  "jwt:boldbiserverurl": "http://localhost:65212",
  "jwt:siteidentifier": "site1",
  "jwt:redirectto": "http://localhost:65212/bi/site/site1",
  "ExpirationTimeInMinutes": "120",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Important:** Replace `<PASTE-SIGNING-KEY-FROM-BOLD-UMS>` with the exact key from Step 1.

### Step 3: Run the Application

```powershell
cd "d:\iframe embed server\samples\Scenario Based Samples\Iframe Full Server Embedding\IframeFullServer"
dotnet run --launch-profile http
```

### Step 4: Test

1. Open: `http://localhost:44383/Home/Embed`
2. Click **Login**, enter an email
3. Verify iframe loads Bold Reports successfully

**Expected flow:**
- Login form → JWTLogin → POST to Bold UMS → Bold Reports renders in iframe

---

## Cloudflare Tunnel Testing

Use this to test with AKS-hosted Bold Reports from your Windows machine.

### Step 1: Install cloudflared

```powershell
# Using Chocolatey
choco install cloudflared

# Or download from: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/installation/
```

### Step 2: Update appsettings.json for AKS Bold Reports

```json
{
  "jwt:signingkey": "<AKS-BOLD-UMS-SIGNING-KEY>",
  "user:userid": "1",
  "jwt:boldbiserverurl": "https://yokogawa-testing-upgrade-site.boldbidemo.com",
  "jwt:siteidentifier": "reports1",
  "jwt:redirectto": "https://yokogawa-testing-upgrade-site.boldbidemo.com/bi/site/reports1",
  "ExpirationTimeInMinutes": "120"
}
```

### Step 3: Start the App and Tunnel

**Terminal 1:**
```powershell
cd "d:\iframe embed server\samples\Scenario Based Samples\Iframe Full Server Embedding\IframeFullServer"
dotnet run --launch-profile http
```

**Terminal 2:**
```powershell
cloudflared tunnel --url http://localhost:44383
```

Copy the tunnel URL (e.g., `https://abc-def-123.trycloudflare.com`)

### Step 4: Configure Bold UMS (AKS)

In your AKS Bold UMS (`https://yokogawa-testing-upgrade-site.boldbidemo.com/ums`):

1. Go to **Settings → Authentication → JWT SSO**
2. Configure:
   - **Remote Login URL**: `https://abc-def-123.trycloudflare.com/Home/JWTLogin`
   - **Remote Logout URL**: `https://abc-def-123.trycloudflare.com/Home/JWTLogout`
   - **Signing Key**: Must match `jwt:signingkey` in appsettings.json
3. In **Settings → Embed Settings**, add allowed origin:
   - `https://abc-def-123.trycloudflare.com`
4. Save

### Step 5: Test

Open the tunnel URL: `https://abc-def-123.trycloudflare.com/Home/Embed`

---

## AKS Deployment

### Step 1: Prerequisites Check

```powershell
# Verify tools
az --version
kubectl version --client
docker --version

# Login to Azure
az login
az account set --subscription "<your-subscription-id>"
```

### Step 2: Prepare Azure Resources

```powershell
# Set variables (customize these)
$RG="boldbi-resources"
$LOCATION="eastus"
$ACR_NAME="boldbiacr"              # Must be globally unique, lowercase, no hyphens
$AKS_NAME="boldbi-aks"

# Create resource group (if needed)
az group create --name $RG --location $LOCATION

# Create ACR (if needed)
az acr create --resource-group $RG --name $ACR_NAME --sku Basic

# Get AKS credentials
az aks get-credentials --resource-group $RG --name $AKS_NAME --overwrite-existing

# Attach ACR to AKS (if not already done)
az aks update --resource-group $RG --name $AKS_NAME --attach-acr $ACR_NAME
```

### Step 3: Configure Kubernetes Manifests

#### Edit k8s/configmap.yaml

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: iframefullserver-config
  namespace: boldbi-embed
data:
  jwt__boldbiserverurl: "https://yokogawa-testing-upgrade-site.boldbidemo.com"
  jwt__siteidentifier: "reports1"
  jwt__redirectto: "https://yokogawa-testing-upgrade-site.boldbidemo.com/bi/site/reports1"
  user__userid: "497b511d-83b6-4f7d-bf77-e22d753cc569"
  ExpirationTimeInMinutes: "120"
```

#### Edit k8s/secret.yaml

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: iframefullserver-secrets
  namespace: boldbi-embed
type: Opaque
stringData:
  jwt__signingkey: "<YOUR-BOLD-UMS-SIGNING-KEY>"
```

**Security Note:** For production, use Azure Key Vault or Kubernetes Secrets with encryption at rest.

#### Edit k8s/deployment.yaml

Update the image path:

```yaml
spec:
  containers:
    - name: web
      image: boldbiacr.azurecr.io/iframefullserver:1.0.0  # ← Update with your ACR name
```

#### Edit k8s/ingress.yaml

```yaml
spec:
  tls:
    - hosts:
        - embed.yourdomain.com  # ← Your public hostname
      secretName: iframefullserver-tls
  rules:
    - host: embed.yourdomain.com  # ← Your public hostname
```

### Step 4: Build and Push Docker Image

```powershell
# Set variables
$ACR_NAME="boldbiacr"
$ACR_LOGIN="$ACR_NAME.azurecr.io"
$IMAGE="$ACR_LOGIN/iframefullserver:1.0.0"

# Login to ACR
az acr login --name $ACR_NAME

# Navigate to project folder
cd "d:\iframe embed server\samples\Scenario Based Samples\Iframe Full Server Embedding\IframeFullServer"

# Build image
docker build -t $IMAGE .

# Push to ACR
docker push $IMAGE
```

### Step 5: Create TLS Certificate

**Option A: Let's Encrypt with cert-manager**

```powershell
# Install cert-manager (if not already installed)
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Create ClusterIssuer (one-time setup)
kubectl apply -f - <<EOF
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: your-email@example.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF
```

Update `k8s/ingress.yaml` to add cert-manager annotation:

```yaml
metadata:
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
```

**Option B: Manual certificate**

```powershell
kubectl create secret tls iframefullserver-tls `
  --namespace boldbi-embed `
  --key privkey.pem `
  --cert fullchain.pem
```

### Step 6: Deploy to AKS

```powershell
# Apply manifests
kubectl apply -f .\k8s\namespace.yaml
kubectl apply -f .\k8s\configmap.yaml
kubectl apply -f .\k8s\secret.yaml
kubectl apply -f .\k8s\deployment.yaml
kubectl apply -f .\k8s\service.yaml
kubectl apply -f .\k8s\ingress.yaml

# Check deployment status
kubectl get pods -n boldbi-embed
kubectl get ingress -n boldbi-embed

# View logs
kubectl logs -n boldbi-embed -l app=iframefullserver --tail=50 -f
```

### Step 7: Configure DNS

Get the Ingress external IP:

```powershell
kubectl get ingress -n boldbi-embed -o wide
```

Create an A record:
- **Name**: `embed` (or your chosen subdomain)
- **Type**: `A`
- **Value**: `<EXTERNAL-IP from above>`

Wait for DNS propagation (1-5 minutes).

### Step 8: Configure Bold UMS (AKS)

In Bold UMS:

1. Go to **Settings → Authentication → JWT SSO**
2. Update:
   - **Remote Login URL**: `https://embed.yourdomain.com/Home/JWTLogin`
   - **Remote Logout URL**: `https://embed.yourdomain.com/Home/JWTLogout`
   - **Signing Key**: Must match `k8s/secret.yaml`
3. In **Settings → Embed Settings**, add:
   - `https://embed.yourdomain.com`
4. Save

### Step 9: Test

Open: `https://embed.yourdomain.com/Home/Embed`

---

## Troubleshooting

### Issue: Blank iframe

**Check:**
1. Browser DevTools → Console for errors
2. Network tab: Look for failed POST to `/ums/sso/jwt/callback`
3. Verify `jwt:boldbiserverurl` is correct
4. Check Bold UMS logs for JWT validation errors

### Issue: 400 Bad Request on JWT callback

**Causes:**
- Signing key mismatch (most common)
- Site identifier doesn't exist
- System clock skew
- Token expired

**Fix:**
```powershell
# Verify signing key matches
kubectl get secret iframefullserver-secrets -n boldbi-embed -o jsonpath='{.data.jwt__signingkey}' | base64 -d

# Check Bold UMS JWT settings match
```

### Issue: Cookie not persisting in iframe

**Causes:**
- Not using HTTPS
- `SameSite=None` not set
- Browser blocking third-party cookies

**Fix:**
- Ensure both apps use HTTPS
- Check `Program.cs` has `SameSite=None` and `Secure=Always`
- Test in Incognito mode

### Issue: localhost URLs in AKS

**Cause:** `UseForwardedHeaders` not configured or placed after `UseHttpsRedirection`

**Fix:** Verify `Program.cs`:
```csharp
app.UseForwardedHeaders(...);  // Must be BEFORE UseHttpsRedirection
app.UseHttpsRedirection();
```

### Issue: Environment variables not working

**Check:**
```powershell
# Verify ConfigMap is applied
kubectl get configmap iframefullserver-config -n boldbi-embed -o yaml

# Check pod environment
kubectl exec -n boldbi-embed -it <pod-name> -- env | grep jwt
```

### Issue: DNS not resolving

```powershell
# Check Ingress IP
kubectl get ingress -n boldbi-embed

# Test DNS
nslookup embed.yourdomain.com

# If using Azure DNS, verify zone and records
az network dns record-set a list --resource-group $RG --zone-name yourdomain.com
```

---

## Summary

### Local Testing
✅ Update `appsettings.json`  
✅ Run `dotnet run`  
✅ Configure Bold UMS with `http://localhost:44383`

### Cloudflare Tunnel
✅ Update `appsettings.json` with AKS Bold URL  
✅ Run `cloudflared tunnel --url http://localhost:44383`  
✅ Configure Bold UMS with tunnel URL

### AKS Deployment
✅ Build and push Docker image to ACR  
✅ Edit K8s manifests with your values  
✅ Deploy with `kubectl apply`  
✅ Configure DNS and TLS  
✅ Update Bold UMS with AKS app URL

---

## Key Points

1. **No code changes needed** between environments—only configuration
2. **Signing key must match exactly** between sample app and Bold UMS
3. **HTTPS required** for cross-domain iframe scenarios
4. **Environment variables override** appsettings.json in containers
5. **UseForwardedHeaders must come first** in Program.cs middleware pipeline

---

## Support

For issues:
- Check Bold Reports logs: `/app/logs` (container) or Bold BI logs folder (Windows)
- Check app logs: `kubectl logs -n boldbi-embed -l app=iframefullserver`
- Verify DevTools Network tab for HTTP status codes and payloads
