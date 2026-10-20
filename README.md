# BAUMARKET VORA Stock Bridge

Agjent Windows i veçantë për sinkronizimin e stokut nga Finance 5 SQL Server në API-në Unionline. Ky projekt është i ndarë nga KRAFT.

## Ndërtimi i EXE-së përmes GitHub Actions

1. Ngarko të gjithë përmbajtjen e këtij ZIP-i në një repository të ri GitHub (ose zëvendëso vetëm skedarët e repository-t të agjentit BAUMARKET_VORA; mos ndrysho repository-n KRAFT).
2. Hape **Actions** dhe zgjidh **Build BAUMARKET VORA EXE**.
3. Kliko **Run workflow**.
4. Pas përfundimit, hap run-in dhe shkarko artifact-in `BauVoraStockBridge-win-x64`.
5. Ekstrakto ZIP-in e artifact-it. Vendos `appsettings.json` në të njëjtën dosje me `BauVoraStockBridge.exe`.

## Konfigurimi

Kopjo `windows-bridge/appsettings.bauvora.example.json` si `appsettings.json` pranë EXE-së dhe plotëso:
- `SqlConnectionString`: lidhja e saktë SQL Server.
- `ApiToken`: token-i real i API-së.
- `ApiUrl`: aktualisht `https://ebaumarket.com/unionline/api/sync.php`.

`appsettings.json` nuk duhet të ngarkohet në GitHub me kredenciale reale. Përdor vetëm shembuj pa sekrete në repository.

Query kthen kolonat që pret agjenti: `ProductCode`, `Barcode`, `ProductName`, `StockQty`, `Price`, `PurchasePriceWithVat`, `SalePriceWithVat`. Stoku llogaritet nga `LEVIZJEHD` për magazinën `MQ` si `SUM(SASIH-SASID)`. Verifiko në Finance 5 se kjo formulë përputhet me raportin e gjendjes dhe nëse `KOSTMES`/`CMSH` janë çmimet e kërkuara me TVSH.

Log-u shkruhet te `logs/bridge.log` pranë EXE-së. Agjenti provon sinkronizimin menjëherë dhe pastaj çdo 5 minuta.
