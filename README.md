# service-rinexmetadatacontroller

Analyzes rinex data and stores the metadata in a SQLite DB and in an XML file.

## Summary

The service analyzes rinex 3.03 data and stores the metadata in a SQLite DB and in an XML.
All configuration parameters are listed in the configuration file App.config.
The rinexparsing is based on the repo sharov-am/Rinex-3.0-Parser. The adapted version is in forked into the repo Juerg-Liechti/Rinex-3.0-Parser.

## Dependencies

Prerequisites for development:
  * c# .Net Framework 4.5
  * System.Data.SQLite.Core -Version 1.0.111 (The official SQLite database engine for both x86 and x64 along with the ADO.NET provider.)
  * Rinex3Parser.dll (from https://github.com/Juerg-Liechti/Rinex-3.0-Parser)
  * crx2rnx.exe (from https://terras.gsi.go.jp/ja/crx2rnx.html)

To get the System.Data.SQLite.Core Package use nuget. PM> Install-Package System.Data.SQLite.Core -Version 1.0.111

## Flow Chart
### Processing a single day of a single input directory

```mermaid
flowchart TD
  Start --> IsZipped{Does input directory contain zipped files?}
  IsZipped -- Yes --> Unzip[Unzipping each zip file into a separate folder inside the working directory]
  Unzip --> CheckUnzipped[Check and fix filenames inside unzipped folder]
  CheckUnzipped --> CopyToShopPath[OPTIONAL: copy to daily shop directory]
  CheckUnzipped --> UncompressHat[Uncompress hatanaka files in working directories]
  UncompressHat --> CopyToSame[Copy all uncompressed to same RNX directory in working directory, delete Hatanaka files]
  IsZipped -- No --> CopyDirect[Copy to shortname.dafi folder and to RNX folder]
  CopyDirect --> AnalyseRinex[Analyse all rinex files in RNX folder, write to MetaDB]
  AnalyseRinex --> WriteGDHW[OPTIONAL: Write GDWH xml file to unzipped folder]
  CopyToSame --> AnalyseRinex
  AnalyseRinex --> DeleteTempRinex[Delete all rinex files in RNX folder and delete RNX folder]
  DeleteTempRinex --> DeleteUncompleteHours[Treate uncomplete Hours unzipped folder, rename folder to full hour]
  DeleteUncompleteHours --> ZipResult[Zip the unzipped folder to the archive path]
  ZipResult --> DeleteInputData[Delete the input data files, depending on settings]
  DeleteInputData --> DeleteWorkginDirectory[Delete working directory]

```
