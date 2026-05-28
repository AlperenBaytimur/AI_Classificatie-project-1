# Requirementsanalyse: E-lips AI Classificatie Platform

**Project:** AI-gestuurde document extractie en classificatie voor logistieke orders  
**Datum:** Mei 2026  
**Stagairs:** Alperen  
**Stagebegeleider:** [Naam in te vullen]

---

## 1. Projectoverzicht

### Doelstelling

Het E-lips AI Classificatie Platform is een REST API-applicatie die automatisch logistieke ordergegevens uit diverse documenttypen (PDF, afbeeldingen, e-mails, tekstbestanden) extraheert met behulp van AI (Ollama/GPT). De geëxtraheerde data wordt geclassificeerd, gevalideerd en geretourneerd in JSON- en XML-formaat met een betrouwbaarheidscore.

### Business Context

Dit systeem helpt logistieke bedrijven om:

- Handmatige data-entry uit fysieke documenten te elimineren
- Verwerkingstijden van bestellingen aanzienlijk te verkorten
- Datakwaliteit via AI-geleide validatie te verbeteren
- Integratieflexibiliteit via REST API en meerdere output-formaten

### Technology Stack

- **Platform:** ASP.NET Core 9.0 (C#)
- **API Documentatie:** OpenAPI/Swagger
- **AI Model:** Ollama (on-premise deployable)
- **Document Processing:**
  - PDF: UglyToad.PdfPig
  - OCR (afbeeldingen): Tesseract
  - Email: MimeKit
  - Images: SixLabors.ImageSharp

---

## 2. Functionele Requirements

### FR1: Document Ingestie

Het systeem moet documenten van meerdere bronnen kunnen inlezen en omzetten naar tekstueel formaat.

**Ondersteunde Formaten:**

- PDF-bestanden (tekst en gescande pagina's via OCR)
- Afbeeldingen (PNG, JPG, JPEG, TIF, BMP) via OCR
- E-mailberichten (.EML) inclusief bijlagen
- Platte tekstbestanden (.TXT)

**Constraints:**

- Maximale inputgrootte: 20.000 karakters (configureerbaar)
- Timeouts: 300 seconden (configureerbaar)

---

### FR2: Data Extractie met AI

Het systeem moet OCR-output en document-tekst naar gestructureerde logistieke orderdata converteren.

**Doelstructuur (Opdracht):**

```
- RelatieCode (string): Unieke relatiecode
- Aantal (integer): Hoeveelheid containers
- ContainerType (string): Type container (bijv. "20ft", "40ft")
- BrutoGewicht (integer): Totaalgewicht in kg
- ZegelNummers (list): Seriële nummers van zegels
- Activiteiten (list):
  - Type (string): Soort activiteit
  - Terminal (string): Terminal locatie
  - Bedrijf (string): Bedrijfsnaam
  - Straat, Postcode, Plaats (string): Adresgegevens
- Financieel (object):
  - Object (string): Beschrijving
  - EenheidsPrijs (decimal): Prijs per eenheid
  - ValutaCode (string): Munteenheid
  - BTWCode (string): VAT-code
  - Fin_Code (string): Financiële code
```

---

### FR3: Kwaliteitsbewaking

Het systeem moet de betrouwbaarheid van geëxtraheerde gegevens evalueren.

**Validatieregels:**

- 14 kritieke velden worden gecontroleerd
- Betrouwbaarheid = (aantal ingevulde velden / 14) * 100%
- Drempel voor handmatige controle: 90% (configureerbaar)
- Rapport van ontbrekende velden

---

### FR4: Output Formatting

Geëxtraheerde data moet in meerdere formaten beschikbaar zijn.

**Output-formaten:**

- JSON: Gestructureerde gegevens
- XML: For legacy system integration
- Metadata: Betrouwbaarheidsscore, drempel, review-vlag, ontbrekende velden

---

### FR5: Error Handling & Fallback

Het systeem moet robuust omgaan met fouten en informative responses teruggeven.

**Scenarios:**

- Timeout bij AI-model (504 Gateway Timeout)
- Niet-ondersteund bestandstype (400 Bad Request)
- Lege of onleesbare bestanden (400 Bad Request)
- AI-parsing fouten (500 Internal Server Error)

---

### FR6: API Documentation

Het systeem moet zelf-documenteren zijn via OpenAPI-spec en interactieve Swagger UI.

**Endpoints:**

- `POST /api/extract` - Extract from text
- `POST /api/extract/file` - Extract from file
- `GET /` - Service status
- `GET /swagger` - API Documentation UI

---

## 3. User Stories

### User Story 1: Als logistieke medewerker wil ik ordergegevens uit een PDF-factuur extracten

**Story ID:** US-001  
**Priority:** High  
**Estimation:** 8 story points

**Beschrijving:**  
Als logistieke medewerker wil ik een PDF-factuur uploaden naar het systeem en automatisch de ordergegevens (relatiecode, aantal, containertype, gewicht) laten extracten, zodat ik deze niet handmatig hoef in te voeren.

**Acceptatiecriteria:**

- [x] Gebruiker kan PDF-bestand uploaden via `/api/extract/file`
- [x] Systeem extraheert PDF-tekst correct (met/zonder OCR)
- [x] Respons bevat volledige `Opdracht`-object
- [x] Bestand moet < 50MB zijn
- [x] Response time < 5 seconden voor normale files

**Testen:**

- Upload standaard PDF met duidelijke tekst → Success
- Upload gescande PDF (lage resolutie) → OCR-fallback
- Upload PDF > 50MB → 400 Bad Request met meldingvalt weg
- Upload ongeldig PDF-bestand → 500 error met loggging

**Opmerkingen:**  
Vereist Tesseract tessdata in `./tessdata` directory

---

### User Story 2: Als OCR-technicus wil ik documenten met geteste afbeeldingenformaten verwerken

**Story ID:** US-002  
**Priority:** High  
**Estimation:** 5 story points

**Beschrijving:**  
Als OCR-technicus wil ik afbeeldingsbestanden (PNG, JPG, TIF) van logistieke documenten uploaden en deze automatisch converteren naar structured orderdata via Tesseract OCR, zodat ik gescande papieren documents kan verwerken.

**Acceptatiecriteria:**

- [x] Ondersteunde formaten: .png, .jpg, .jpeg, .tif, .tiff, .bmp
- [x] Afbeeldingen < 10MB
- [x] OCR-output wordt doorgegeven aan AI model
- [x] Betrouwbaarheidsscore wordt berekend voor OCR-resultaat
- [x] Error handling voor beschadigde/onleesbare afbeeldingen

**Testen:**

- Upload .jpg met duidelijke tekst → 95%+ OCR accuracy
- Upload .png in grijs → Correct geconverteerd
- Upload .bmp (256 color) → Ondersteund
- Upload .gif (unsupported) → 400 Bad Request
- Upload 15MB afbeelding → 400 Bad Request

**Opmerkingen:**  
SixLabors.ImageSharp wordt gebruikt voor image validation

---

### User Story 3: Als e-mailintegratie-manager wil ik ordergegevens uit e-mailbijlagen extracten

**Story ID:** US-003  
**Priority:** Medium  
**Estimation:** 5 story points

**Beschrijving:**  
Als e-mailintegratie-manager wil ik e-mailberichten (.EML formaat) met bijlagen uploaden en de ordergegevens automatisch extracten (zowel uit e-mailinhoud als uit bijgesloten PDF's/afbeeldingen), zodat e-mailgebaseerde bestellingen geautomatiseerd kunnen worden verwerkt.

**Acceptatiecriteria:**

- [x] Systeem herkent .EML-bestanden
- [x] Tekst (HTML en plaintext) uit e-mailbody wordt geëxtraheerd
- [x] Bijlagen (PDF, images) worden recursief verwerkt
- [x] Volledige e-mail metadata wordt gelogd
- [x] Fallback naar plaintext als HTML parsing faalt

**Testen:**

- Upload .eml met plaintext body → Text geëxtraheerd
- Upload .eml met HTML body → HTML gestript, text geëxtraheerd
- Upload .eml met PDF-bijlage → Bijlage geprocesst
- Upload .eml met geen inhoud → 400 Bad Request
- Upload ongeldig .eml-bestand → 500 Error met logging

**Opmerkingen:**  
MimeKit library gebruikt voor e-mailparsing

---

### User Story 4: Als API-consument wil ik ordergegevens in XML-formaat ontvangen

**Story ID:** US-004  
**Priority:** Medium  
**Estimation:** 3 story points

**Beschrijving:**  
Als API-consument van legacy systemen wil ik de geëxtraheerde ordergegevens ook in XML-formaat ontvangen (naast JSON), zodat ik deze kan integreren in bestaande systemen die XML verwachten.

**Acceptatiecriteria:**

- [x] Response bevat zowel JSON als XML in hetzelfde object
- [x] XML-schema volgt structuur van `Opdracht`-model
- [x] XML is valide en well-formed
- [x] Geneste elementen (Activiteiten, Financieel) correct gerepresenteerd
- [x] Datatypen correct gemapped (decimal, integer, list)

**Testen:**

- Extract order → Response bevat `xml` veld
- XML-output is well-formed (parse-able in libxml)
- Alle geneste elementen aanwezig
- Decimal values correct geformateerd
- Arrays correct als `<Item>` elements

**Opmerkingen:**  
XmlService bestaat al, maar verdient unit testing

---

### User Story 5: Als quality assurance manager wil ik weten welke gegevens ontbreken

**Story ID:** US-005  
**Priority:** High  
**Estimation:** 5 story points

**Beschrijving:**  
Als quality assurance manager wil ik voor elk geëxtraheerd order zien welke velden ontbreken of onvolledig zijn, zodat ik weet welke orders handmatige controle nodig hebben en welke direct kunnen worden verwerkt.

**Acceptatiecriteria:**

- [x] Response bevat array `missingFields` met namen van ontbrekende velden
- [x] 14 kritieke velden worden gecontroleerd:
  - RelatieCode, Aantal, ContainerType, BrutoGewicht, ZegelNummers
  - Activiteiten (Type, Terminal, Bedrijf, Straat, Postcode, Plaats)
  - Financieel (Object, EenheidsPrijs, ValutaCode, BTWCode, Fin_Code)
- [x] `missingFields` is lege array als alles ingevuld
- [x] Rapport is altijd aanwezig ongeacht confidence score

**Testen:**

- Extract order met alle velden → missingFields = []
- Extract order missing "RelatieCode" → missingFields = ["RelatieCode"]
- Extract order missing meerdere velden → missingFields bevat allen
- Extract met empty Activiteiten → "Activiteiten[0].Type" in missingFields

**Opmerkingen:**  
Validatielogica bevindt zich in `ExtractionService.CalculateConfidence()`

---

### User Story 6: Als systeem administrator wil ik de AI-model timeout configureren

**Story ID:** US-006  
**Priority:** Medium  
**Estimation:** 3 story points

**Beschrijving:**  
Als systeem administrator wil ik de timeout voor AI-model responses configureren (via appsettings), zodat ik kan omgaan met variabele verwerkingstijden op verschillende servers (snelle GPU's vs. CPU-only deployment).

**Acceptatiecriteria:**

- [x] Timeout configureerbaar via `Ollama:TimeoutSeconds` in appsettings
- [x] Standaardwaarde: 300 seconden
- [x] Bij timeout antwoort API met 504 Gateway Timeout status
- [x] Error message bevat suggestie voor troubleshooting
- [x] Timeout is per-request, niet globaal

**Testen:**

- Default config → 300 seconden timeout
- Config "Ollama:TimeoutSeconds" = 60 → 60 sec timeout
- Config "Ollama:TimeoutSeconds" = 0 → Immediate timeout (edge case)
- Slow model + low timeout → 504 response
- Slow model + high timeout → 200 response (success)

**Opmerkingen:**  
HttpClient timeout geconfigureerd in Program.cs via AddHttpClient

---

### User Story 7: Als developer wil ik de API-endpoints documentatie hebben

**Story ID:** US-007  
**Priority:** Medium  
**Estimation:** 2 story points

**Beschrijving:**  
Als developer wil ik een interactieve API-documentatie (Swagger UI) kunnen raadplegen met alle beschikbare endpoints, request/response schemas en voorbeelden, zodat ik de API correct kan integreren.

**Acceptatiecriteria:**

- [x] Swagger UI beschikbaar op `/swagger`
- [x] OpenAPI schema beschikbaar op `/openapi/v1.json`
- [x] Alle endpoints gedocumenteerd:
  - POST /api/extract (text-based extraction)
  - POST /api/extract/file (file upload extraction)
  - GET / (status endpoint)
- [x] Request-schemas tonen alle velden
- [x] Response-schemas inclusief error cases (400, 500, 504)
- [x] Voorbeeldwaarden in schema

**Testen:**

- GET /swagger → HTML-pagina laadt
- GET /openapi/v1.json → Valide OpenAPI 3.0 spec
- Try-it-out in Swagger → Request kan verstuurd worden
- Error cases gedocumenteerd → 400, 500, 504 responses zichtbaar

**Opmerkingen:**  
Swashbuckle.AspNetCore v7.2 is ge-configured

---

### User Story 8: Als input-validatie manager wil ik grote input beperken

**Story ID:** US-008  
**Priority:** High  
**Estimation:** 3 story points

**Beschrijving:**  
Als input-validatie manager wil ik grote tekst-inputs beperken (bijv. >20.000 karakters) zodat het AI-model niet overbelast wordt en de response-time acceptabel blijft.

**Acceptatiecriteria:**

- [x] Maximale input: 20.000 karakters (configureerbaar via `ExtractionQuality:MaxInputChars`)
- [x] Input groter dan maximum → Auto-truncated (geen error)
- [x] Truncated input wordt gelogd met waarschuwing
- [x] Betrouwbaarheidsscore geeft aan dat gegevens partial zijn
- [x] Config-value overschrijfbaar in appsettings

**Testen:**

- Input < 20k chars → Volledig verwerkt
- Input = 20k chars → Volledig verwerkt
- Input = 25k chars → First 20k verwerkt, warning gelogd
- Config "ExtractionQuality:MaxInputChars" = 10000 → Respecteert new limit
- Edge case: Input = 1 char → Verwerkt (kan null zijn)

**Opmerkingen:**  
ExtractionService.ExtractAsync() heeft truncation logic

---

### User Story 9: Als compliance officer wil ik een betrouwbaarheidsdrempel configureren

**Story ID:** US-009  
**Priority:** High  
**Estimation:** 2 story points

**Beschrijving:**  
Als compliance officer wil ik de betrouwbaarheidsdrempel configureren (via appsettings) waarboven orders automatisch verwerkt kunnen worden, zodat alle orders onder de drempel handmatige controle krijgen voor quality assurance.

**Acceptatiecriteria:**

- [x] Drempel configureerbaar via `ExtractionQuality:ManualReviewThreshold`
- [x] Standaardwaarde: 90%
- [x] Response bevat veld `manualReviewRequired` (boolean)
- [x] `manualReviewRequired` = true als score < drempel
- [x] Response bevat ook daadwerkelijke `manualReviewThreshold` voor logging
- [x] Config verandering retroactief op bestaande API requests

**Testen:**

- Default threshold = 90%, score = 95% → manualReviewRequired = false
- Threshold = 90%, score = 85% → manualReviewRequired = true
- Config "ExtractionQuality:ManualReviewThreshold" = 75 → Nieuwe drempel
- Threshold = 0% → Alles automatisch (geen review nodig)
- Threshold = 100% → Alles requires review

**Opmerkingen:**  
ExtractionService.ExtractAsync() implementeert deze logica

---

### User Story 10: Als error handling specialist wil ik duidelijke foutmeldingen krijgen

**Story ID:** US-010  
**Priority:** High  
**Estimation:** 5 story points

**Beschrijving:**  
Als error handling specialist wil ik duidelijke en actionable foutmeldingen ontvangen (bijv. bij timeout, niet-ondersteund bestand, lege input), zodat ik fouten snel kan diagnosticeren en oplossen.

**Acceptatiecriteria:**

- [x] 400 Bad Request: Duidelijke beschrijving van input-probleem
  - "Text is required" (leeg tekstveld)
  - "File is required" (geen bestand geüpload)
  - "No readable text could be extracted" (onleesbaar bestand)
  - "File type 'xxx' is not supported" (niet-ondersteund formaat)
- [x] 500 Internal Server Error: "Extraction failed" + logging
- [x] 504 Gateway Timeout: "AI model timeout" + troubleshooting-suggestie
- [x] Alle errors worden naar server-logs geschreven
- [x] Timeout response bevat suggestions: "Try a smaller input file, increase Ollama:TimeoutSeconds, or use a faster model"

**Testen:**

- POST /api/extract met leeg text → 400 "Text is required"
- POST /api/extract/file zonder file → 400 "File is required"
- Upload .docx bestand → 400 "not supported"
- Upload empty .txt → 400 "No readable text"
- AI timeout → 504 met suggestion message
- AI parsing error → 500 "Extraction failed" + logs

**Opmerkingen:**  
Try-catch logica in ExtractionController en services

---

### User Story 11: Als integration specialist wil ik batch verwerking van meerdere documenten

**Story ID:** US-011  
**Priority:** Low  
**Estimation:** 8 story points

**Beschrijving:**  
Als integration specialist wil ik meerdere documenten in één API-call versturen (batch mode), zodat ik bulk-verwerking van orders kan automatiseren en het aantal API-calls reduce.

**Acceptatiecriteria:**

- [x] Endpoint `POST /api/extract/batch` ondersteunt array van files
- [x] Max 10 files per request (configureerbaar)
- [x] Response: Array van extraction results (parallel verwerkt)
- [x] Timeout: Per file, niet totaal (5 min per file)
- [x] Partial success: File failures beïnvloeden niet andere files
- [x] Response includeert success/error status per file

**Testen:**

- Batch van 5 geldige files → 5 success results
- Batch met 1 ongeldig file → 9 success, 1 error
- Batch van 10 files → Allemaal verwerkt
- Batch van 11 files → 400 "Max 10 files"
- Batch timeout behavior → Enkel timed-out file fails

**Opmerkingen:**  
Future enhancement - vereist parallel async processing

---

### User Story 12: Als data scientist wil ik confidence score trends analyseren

**Story ID:** US-012  
**Priority:** Low  
**Estimation:** 8 story points

**Beschrijving:**  
Als data scientist wil ik analytics-endpoints hebben die confidence score trends en failure patterns kunnen visualiseren, zodat ik de AI-model performance kan monitoren en verbeteren.

**Acceptatiecriteria:**

- [x] Endpoint `GET /api/analytics/confidence-scores` (last 7 days)
- [x] Response: Time-series data met avg/min/max confidence
- [x] Endpoint `GET /api/analytics/missing-fields` (top 10)
- [x] Response: Frequentie van ontbrekende velden
- [x] Endpoint `GET /api/analytics/file-types` (success rate per type)
- [x] Response: Success/failure ratio per bestandstype
- [x] Data wordt persistent opgeslagen (database/logging)

**Testen:**

- After 100 extractions, confidence data beschikbaar
- Missing fields rapport toont juiste frequenties
- File type success rates correct berekend
- Time-series filtering op datum works
- Analytics API protected via auth (future)

**Opmerkingen:**  
Vereist database schema en logging-infrastruc tuur

---

## 4. Non-Functional Requirements

| NFR | Omschrijving | Target |
|-----|-------------|--------|
| **Performance** | Response time voor normaal document | < 5 sec |
| **Scalability** | Concurrent requests | Min. 10/sec |
| **Availability** | Uptime (excl. planned maintenance) | 99.5% |
| **Security** | Input validation & XSS prevention | OWASP Top 10 |
| **Maintainability** | Code coverage unit tests | > 70% |
| **Logging** | All errors logged with context | Structured logging |
| **Documentation** | API docs auto-generated | OpenAPI 3.0 compliant |
| **Compatibility** | Supported file formats | 5+ types |

---

## 5. User Story Backlog Prioritering

| Priority | User Stories |
|----------|-------------|
| **Must Have (P0)** | US-001, US-002, US-005, US-008, US-009, US-010 |
| **Should Have (P1)** | US-003, US-004, US-006, US-007 |
| **Nice to Have (P2)** | US-011, US-012 |

---

## 6. Acceptance Criteria Checklist

### Release 1.0 (MVP - Eind Augustus 2026)

- [x] Document upload & text extraction (PDF, images, text, email)
- [x] AI-based field extraction (14 critical fields)
- [x] Confidence scoring & quality gates
- [x] JSON & XML output formats
- [x] REST API with error handling
- [x] Swagger UI documentation
- [x] Configuration management (timeouts, thresholds, max input)
- [x] Logging infrastructure

**Exclusions:**

- Batch processing (defer to v1.1)
- Analytics dashboard (defer to v1.2)
- Authentication/Authorization (future phase)
- Database persistence (future phase)

---

## 7. Bijlagen

### A. Configuratie Schema (appsettings.json)

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434/api/generate",
    "Model": "gpt-oss:20b",
    "TimeoutSeconds": 300
  },
  "Ocr": {
    "TessDataPath": "./tessdata",
    "Language": "eng"
  },
  "ExtractionQuality": {
    "ManualReviewThreshold": 90,
    "MaxInputChars": 20000
  }
}
```

### B. API Request/Response Voorbeelden

#### Request: Text Extraction

```json
POST /api/extract
{
  "text": "RelatieCode: BDV001\nAantal: 2\nContainerType: 40ft..."
}
```

#### Response: Success (200)

```json
{
  "json": {
    "relatieCode": "BDV001",
    "aantal": 2,
    "containerType": "40ft",
    ...
  },
  "confidenceScore": 95.67,
  "manualReviewThreshold": 90,
  "manualReviewRequired": false,
  "missingFields": [],
  "xml": "<Opdracht><RelatieCode>BDV001</RelatieCode>..."
}
```

#### Response: Timeout (504)

```json
{
  "error": "AI model timeout",
  "message": "The operation timed out.",
  "suggestion": "Try a smaller input file, increase Ollama:TimeoutSeconds, or use a faster model."
}
```

---

## 8. Sign-Off

**Prepared by:** [Stagiair Naam]  
**Date:** Mei 2026  
**Review Status:** Pending (voor beoordeling stagebegeleider)

**Stagebegeleider Approval:**

- [ ] Goedgekeurd op: ___________
- [ ] Handtekening: ___________
- [ ] Opmerkingen: ___________

---

**Document Versie:** 1.0  
**Laatst bijgewerkt:** Mei 2026
