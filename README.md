# Teradyne ICT Spectrum Converter

Converts Teradyne Spectrum in-circuit test result files to WATS UUT reports.

## Integration Details

| Property | Value |
|----------|-------|
| **Category** | WATS Client converter |
| **Type** | FileConverter |
| **Format** | LOG, TXT |
| **Test type** | ICT |

## About

The Teradyne Spectrum is an in-circuit test system for high-volume PCB assembly testing. This converter imports Spectrum log output into WATS as UUT reports.

## Getting Started

* [What is WATS?](https://wats.com)
* [WATS Client download](https://wats.com/download)
* [Setting up a custom converter](https://support.wats.com/hc/en-us/articles/13344321749788-Setting-up-a-custom-converter)

## Download

The recommended installation method is via the MSI installer. Download the latest release from the [Releases](https://github.com/TheWATSCompany/WATS-Converter-Teradyne-ICT/releases/latest) page.

## Installation

### Using the MSI Installer (Recommended)

1. Download the `.msi` file from the [Releases](https://github.com/TheWATSCompany/WATS-Converter-Teradyne-ICT/releases/latest) page.
2. Run the installer - it will automatically place the converter in the correct WATS Client folder.
3. Restart the WATS Client Service.

### Manual DLL Installation

1. Download the `.dll` file from the [Releases](https://github.com/TheWATSCompany/WATS-Converter-Teradyne-ICT/releases/latest) page.
2. In the WATS Client Configurator, go to Converters, click Add, and browse for the downloaded DLL.
3. Select the appropriate converter class from the drop-down.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `operationTypeCode` | 10 | Operation type code when missing from the log file. |
| `stationName` | | Station name when missing from the log file. |

## Contributing

We welcome contributions! Feel free to open an issue or create a pull request.

## Troubleshooting

### Converter failed to start

* Ensure the WATS Client Service has folder permission to the input path.
* Restart the WATS Client Service after configuration changes.

### Converter class drop-down is empty

* The DLL file may be blocked by Windows. Right-click the file, open Properties, and click Unblock.

### Other issues

Contact [WATS Support](mailto:support@wats.com) and include the `wats.log` file.

## Resources

* [GitHub Repository](https://github.com/TheWATSCompany/WATS-Converter-Teradyne-ICT)
* [Teradyne ICT systems](https://www.teradyne.com)
* [WATS Documentation](https://support.wats.com)
* [Setting up a custom converter](https://support.wats.com/hc/en-us/articles/13344321749788-Setting-up-a-custom-converter)

## License

See [LICENSE](LICENSE.md) for details.
