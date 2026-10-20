# Konfigurimi BAUMARKET VORA

1. Shkarko artifact-in `BauVoraStockBridge-win-x64` nga GitHub Actions.
2. Kopjo `appsettings.bauvora.example.json` në dosjen e EXE-së dhe riemërtoje `appsettings.json`.
3. Plotëso SQL user/password dhe `ApiToken` real. Mos përdor password bosh nëse SQL Server nuk e lejon shprehimisht.
4. Sigurohu që `appsettings.json` është skedar JSON i vlefshëm dhe ndodhet pranë EXE-së.
5. Nis `BauVoraStockBridge.exe` nga ajo dosje. Log-u është te `logs/bridge.log`.

Nëse shfaqet gabim, ruaj tekstin e plotë nga konsola ose nga `logs/bridge.log`.
