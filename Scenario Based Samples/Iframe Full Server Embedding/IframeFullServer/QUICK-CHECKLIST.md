# Quick Deployment Checklist

Use this checklist to ensure everything is configured correctly before deployment.

---

## 🧪 Local Testing Checklist

### Before Running

- [ ] Copied `appsettings.LOCAL-EXAMPLE.json` to `appsettings.json`
- [ ] Got JWT signing key from Bold UMS (`http://localhost:PORT/ums/administration/sso`)
- [ ] Pasted signing key into `appsettings.json` → `jwt:signingkey`
- [ ] Verified `jwt:boldbiserverurl` points to local Bold instance
- [ ] Verified `jwt:siteidentifier` matches Bold site identifier
- [ ] Verified `jwt:redirectto` is correct Bold BI/Reports URL

### Bold UMS Configuration

- [ ] Enabled JWT SSO in Bold UMS
- [ ] Set Remote Login URL: `http://localhost:44383/Home/JWTLogin`
- [ ] Set Remote Logout URL: `http://localhost:44383/Home/JWTLogout`
- [ ] Signing key matches appsettings.json exactly
- [ ] Saved settings in Bold UMS

### Testing

- [ ] Ran `dotnet run --launch-profile http`
- [ ] Opened `http://localhost:44383/Home/Embed`
- [ ] Clicked Login and entered email
- [ ] Iframe loaded Bold Reports successfully
- [ ] No errors in Browser DevTools Console
- [ ] JWT POST returned 302 (not 400 or 500)

---

## 🌐 Cloudflare Tunnel Testing Checklist

### Configuration

- [ ] Installed cloudflared CLI
- [ ] Copied `appsettings.AKS-EXAMPLE.json` to `appsettings.json`
- [ ] Updated `jwt:boldbiserverurl` with AKS Bold Reports URL
- [ ] Updated `jwt:siteidentifier` with AKS site identifier
- [ ] Updated `jwt:redirectto` with AKS Bold Reports landing page
- [ ] Got JWT signing key from AKS Bold UMS
- [ ] Pasted signing key into `appsettings.json`

### Running

- [ ] Started app: `dotnet run --launch-profile http`
- [ ] Started tunnel: `cloudflared tunnel --url http://localhost:44383`
- [ ] Copied tunnel URL (e.g., `https://xyz.trycloudflare.com`)

### AKS Bold UMS Configuration

- [ ] Updated Remote Login URL with tunnel URL + `/Home/JWTLogin`
- [ ] Updated Remote Logout URL with tunnel URL + `/Home/JWTLogout`
- [ ] Verified signing key matches appsettings.json
- [ ] Added tunnel origin to allowed embed domains
- [ ] Saved Bold UMS settings

### Testing

- [ ] Opened tunnel URL + `/Home/Embed`
- [ ] Logged in successfully
- [ ] Iframe loaded AKS Bold Reports
- [ ] No localhost Browser Link errors in Console
- [ ] JWT POST to AKS succeeded (302 redirect)

---

## ☸️ AKS Deployment Checklist

### Prerequisites

- [ ] Azure CLI installed and logged in
- [ ] kubectl installed and configured
- [ ] Docker Desktop installed and running
- [ ] ACR created
- [ ] AKS cluster running with NGINX Ingress
- [ ] DNS zone configured (or will use IP for testing)

### Azure Setup

- [ ] Logged in: `az login`
- [ ] Set subscription: `az account set --subscription "<id>"`
- [ ] Got AKS credentials: `az aks get-credentials --resource-group <rg> --name <aks>`
- [ ] Attached ACR to AKS: `az aks update --attach-acr <acr>`
- [ ] Verified kubectl context: `kubectl config current-context`

### Configuration Files

- [ ] Updated `k8s/configmap.yaml`:
  - [ ] `jwt__boldbiserverurl` = AKS Bold Reports URL
  - [ ] `jwt__siteidentifier` = correct site ID
  - [ ] `jwt__redirectto` = correct landing page URL

- [ ] Updated `k8s/secret.yaml`:
  - [ ] `jwt__signingkey` = EXACT key from Bold UMS (no spaces/newlines)

- [ ] Updated `k8s/deployment.yaml`:
  - [ ] `image:` = `<your-acr>.azurecr.io/iframefullserver:1.0.0`

- [ ] Updated `k8s/ingress.yaml`:
  - [ ] `host:` = your public hostname (e.g., `embed.yourdomain.com`)
  - [ ] `secretName:` = TLS certificate secret name

### Docker Build & Push

- [ ] Logged into ACR: `az acr login --name <acr>`
- [ ] Built image: `docker build -t <acr>.azurecr.io/iframefullserver:1.0.0 .`
- [ ] Pushed image: `docker push <acr>.azurecr.io/iframefullserver:1.0.0`
- [ ] Verified in ACR: `az acr repository list --name <acr>`

### Kubernetes Deployment

- [ ] Applied namespace: `kubectl apply -f k8s/namespace.yaml`
- [ ] Applied ConfigMap: `kubectl apply -f k8s/configmap.yaml`
- [ ] Applied Secret: `kubectl apply -f k8s/secret.yaml`
- [ ] Applied Deployment: `kubectl apply -f k8s/deployment.yaml`
- [ ] Applied Service: `kubectl apply -f k8s/service.yaml`
- [ ] Created TLS secret OR configured cert-manager
- [ ] Applied Ingress: `kubectl apply -f k8s/ingress.yaml`

### Verification

- [ ] Pods running: `kubectl get pods -n boldbi-embed`
- [ ] Pods ready: Shows `2/2` in READY column
- [ ] Service created: `kubectl get svc -n boldbi-embed`
- [ ] Ingress created: `kubectl get ingress -n boldbi-embed`
- [ ] External IP assigned (may take 2-5 minutes)
- [ ] Checked logs: `kubectl logs -n boldbi-embed -l app=iframefullserver`

### DNS Configuration

- [ ] Got Ingress external IP: `kubectl get ingress -n boldbi-embed`
- [ ] Created A record: `embed.yourdomain.com` → `<EXTERNAL-IP>`
- [ ] Waited for DNS propagation (1-5 minutes)
- [ ] Tested DNS: `nslookup embed.yourdomain.com`

### TLS Certificate

- [ ] Certificate issued (if using cert-manager)
- [ ] TLS secret exists: `kubectl get secret iframefullserver-tls -n boldbi-embed`
- [ ] Ingress shows HTTPS endpoint

### Bold UMS Configuration

- [ ] Opened Bold UMS: `https://<aks-bold-host>/ums/administration/sso`
- [ ] Updated Remote Login URL: `https://embed.yourdomain.com/Home/JWTLogin`
- [ ] Updated Remote Logout URL: `https://embed.yourdomain.com/Home/JWTLogout`
- [ ] Verified signing key matches K8s secret
- [ ] Added `https://embed.yourdomain.com` to allowed embed origins
- [ ] Saved Bold UMS settings

### Final Testing

- [ ] Opened: `https://embed.yourdomain.com/Home/Embed`
- [ ] HTTPS works (no certificate warnings)
- [ ] Login page loads
- [ ] Logged in with email
- [ ] Iframe loaded Bold Reports from AKS
- [ ] No errors in Browser DevTools Console
- [ ] JWT POST to Bold succeeded (302 redirect)
- [ ] Bold Reports rendered correctly in iframe
- [ ] Logout works correctly

### Monitoring & Logs

- [ ] Checked pod logs: `kubectl logs -n boldbi-embed -l app=iframefullserver`
- [ ] No errors in application logs
- [ ] Bold Reports logs show successful JWT validation
- [ ] Verified metrics/monitoring if configured

---

## 🐛 Troubleshooting Quick Checks

### If iframe is blank:
- [ ] Check Browser DevTools Console for errors
- [ ] Verify Network tab shows POST to Bold UMS
- [ ] Check HTTP status (400 = config mismatch, 500 = server error)

### If getting 400 on JWT callback:
- [ ] Signing key EXACTLY matches between app and Bold UMS
- [ ] Site identifier exists in Bold UMS
- [ ] System clock is correct (JWT has expiration)
- [ ] Check Bold UMS logs for validation errors

### If cookies not working:
- [ ] Both apps use HTTPS (required for Secure cookies)
- [ ] `SameSite=None` configured in Program.cs
- [ ] Browser allows third-party cookies (test in Incognito)

### If URLs show localhost in AKS:
- [ ] `UseForwardedHeaders` is in Program.cs
- [ ] Placed BEFORE `UseHttpsRedirection`
- [ ] Ingress controller sends X-Forwarded-* headers

### If environment variables not working:
- [ ] ConfigMap applied: `kubectl get cm -n boldbi-embed`
- [ ] Secret applied: `kubectl get secret -n boldbi-embed`
- [ ] Pod restarted after changes: `kubectl rollout restart deployment iframefullserver -n boldbi-embed`
- [ ] Verify in pod: `kubectl exec -it <pod> -n boldbi-embed -- env | grep jwt`

---

## 📊 Success Criteria

### Local Testing
✅ App runs on `http://localhost:44383`  
✅ Iframe loads local Bold Reports  
✅ JWT POST succeeds (302 response)  
✅ No errors in DevTools Console

### Cloudflare Tunnel
✅ Tunnel URL accessible over HTTPS  
✅ Iframe loads AKS Bold Reports  
✅ No localhost references in requests  
✅ JWT POST to AKS succeeds

### AKS Production
✅ Accessible via public HTTPS domain  
✅ TLS certificate valid (no warnings)  
✅ Multiple pods running (replicas=2)  
✅ Health checks passing (readiness/liveness)  
✅ JWT authentication working end-to-end  
✅ Bold Reports render correctly in iframe  
✅ Logout redirects correctly

---

## 📞 Support Resources

- **README.md** - Quick start guide
- **DEPLOYMENT-GUIDE.md** - Complete deployment documentation
- **CHANGES-SUMMARY.md** - List of all code changes
- **Bold BI JWT Docs** - https://help.boldbi.com/security-configuration/single-sign-on/json-web-token/

---

## 🎉 Done!

Once all checkboxes are complete, your application is ready for production use in both Windows and AKS environments!
