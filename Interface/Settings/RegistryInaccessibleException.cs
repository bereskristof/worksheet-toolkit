namespace Interface.Settings;

public class RegistryInaccessibleException() : Exception("The Windows registry path 'HKEY_CURRENT_USER\\SOFTWARE\\WorksheetToolkit' could not be reached.");
