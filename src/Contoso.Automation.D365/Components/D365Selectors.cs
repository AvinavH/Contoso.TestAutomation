namespace Contoso.Automation.D365.Components;

/// <summary>
/// Centralised library of D365 Unified Client Interface (UCI) selectors.
/// All locators are based on stable HTML attributes (data-id, aria-label) rather
/// than CSS classes, which D365 minifies and rotates between releases.
///
/// Reference: Microsoft Power Apps UCI DOM structure (v9.2+)
/// </summary>
public static class D365Selectors
{
    // ─── Navigation ──────────────────────────────────────────────────────────

    /// <summary>Top navigation bar - presence confirms D365 has loaded</summary>
    public const string TopBar = "[id='topBar'], .o365cs-nav-topbar-container";

    /// <summary>Site map navigation panel</summary>
    public const string SiteMap = "[data-id='navbar-container']";

    /// <summary>Hamburger menu to open/close the sitemap</summary>
    public const string SiteMapToggle = "[data-id='navbar-hamburger-button'], button[aria-label='Open navigation menu']";

    /// <summary>Navigation entity link in sitemap. Param: entity logical name (e.g. 'account')</summary>
    public static string SiteMapEntity(string logicalName) =>
        $"[data-id='sitemap-entity-{logicalName}'], li[aria-label*='{logicalName}' i]";

    /// <summary>Application switcher button</summary>
    public const string AppSwitcher = "[data-id='app-switcher-btn'], button[aria-label='Apps']";

    // ─── Forms ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Text input for a field. fieldName is the D365 logical attribute name (e.g. 'name', 'telephone1').
    /// </summary>
    public static string TextInput(string fieldName) =>
        $"[data-id='{fieldName}.fieldControl-text-box-text']";

    /// <summary>Textarea for multiline text fields</summary>
    public static string TextArea(string fieldName) =>
        $"[data-id='{fieldName}.fieldControl-text-box-textarea']";

    /// <summary>Currency field input</summary>
    public static string CurrencyInput(string fieldName) =>
        $"[data-id='{fieldName}.fieldControl-currency-text-input']";

    /// <summary>Option set (dropdown) select element</summary>
    public static string OptionSet(string fieldName) =>
        $"[data-id='{fieldName}.fieldControl-option-set-select']";

    /// <summary>Date/time input</summary>
    public static string DateInput(string fieldName) =>
        $"[data-id='{fieldName}.fieldControl-date-time-input']";

    /// <summary>Boolean (two-option) checkbox input</summary>
    public static string BooleanField(string fieldName) =>
        $"[data-id='{fieldName}.fieldControl-checkbox-container'] input[type='checkbox']";

    /// <summary>Lookup search input box. fieldName = attribute logical name</summary>
    public static string LookupInput(string fieldName) =>
        $"[data-id='{fieldName}.fieldControl-LookupResultsDropdown_{fieldName}_InputSearch']";

    /// <summary>Lookup results container (dropdown panel)</summary>
    public static string LookupResults(string fieldName) =>
        $"[data-id='{fieldName}.fieldControl-LookupResultsDropdown_{fieldName}_ResultSet']";

    /// <summary>Individual result row in a lookup dropdown</summary>
    public static string LookupResultItem(string fieldName) =>
        $"[data-id='{fieldName}.fieldControl-LookupResultsDropdown_{fieldName}_ResultSet'] li";

    /// <summary>Field error/validation message container</summary>
    public static string FieldError(string fieldName) =>
        $"[data-id='{fieldName}.fieldControl-error-message'], p[id*='{fieldName}'][id*='error']";

    /// <summary>Field label - useful for asserting mandatory asterisk is present</summary>
    public static string FieldLabel(string fieldName) =>
        $"label[id*='{fieldName}'][id*='label']";

    // ─── Command Bar ─────────────────────────────────────────────────────────

    /// <summary>The command bar ribbon container</summary>
    public const string CommandBar = "[data-id='CommandBar']";

    /// <summary>New record button</summary>
    public const string NewButton = "[data-id='new-command'], button[aria-label='New']";

    /// <summary>Save button</summary>
    public const string SaveButton = "button[aria-label='Save'], [data-id='save-command']";

    /// <summary>Save and Close button</summary>
    public const string SaveAndCloseButton = "button[aria-label='Save & Close'], [data-id='saveandclose-command']";

    /// <summary>Delete button</summary>
    public const string DeleteButton = "button[aria-label='Delete'], [data-id='DeletePrimaryRecord']";

    /// <summary>Deactivate button</summary>
    public const string DeactivateButton = "button[aria-label='Deactivate']";

    /// <summary>Generic command bar button by its aria-label</summary>
    public static string CommandBarButton(string label) =>
        $"[data-id='CommandBar'] button[aria-label='{label}'], li[aria-label='{label}'] button";

    // ─── Confirmation Dialogs ─────────────────────────────────────────────────

    /// <summary>Generic confirmation dialog OK / Yes button</summary>
    public const string ConfirmButton = "button[data-id='ok_id'], button[aria-label='OK'], button[aria-label='Delete']";

    /// <summary>Cancel button in dialogs</summary>
    public const string CancelButton = "button[data-id='cancel_id'], button[aria-label='Cancel']";

    // ─── Grid / Entity List ───────────────────────────────────────────────────

    /// <summary>Main entity grid container</summary>
    public const string Grid = "[data-id='entityListDataTable'], div[class*='grid-container']";

    /// <summary>Grid row selector. Each row has a data-id containing the record GUID</summary>
    public const string GridRow = "tr[data-id^='entityrecord-datatable-']";

    /// <summary>Anchor link in the primary name column of a grid row</summary>
    public const string GridRowNameLink = "tr[data-id^='entityrecord-datatable-'] a[data-id*='primaryField']";

    /// <summary>Grid row by position (1-based)</summary>
    public static string GridRowByIndex(int index) =>
        $"tr[data-id^='entityrecord-datatable-']:nth-child({index})";

    /// <summary>Quick search input in the grid header</summary>
    public const string GridSearchInput = "input[aria-label='Quick Search'], input[data-id='quickFind-text']";

    /// <summary>Quick search submit button</summary>
    public const string GridSearchButton = "button[aria-label='Start search'], button[data-id='quickFind-button']";

    /// <summary>Loading spinner/progress indicator - wait for this to disappear</summary>
    public const string LoadingSpinner = ".ms-Spinner, [data-id='loading-spinner'], div[class*='progress']";

    // ─── Notifications ────────────────────────────────────────────────────────

    /// <summary>Success notification bar</summary>
    public const string SuccessNotification = "div.ms-MessageBar.ms-MessageBar--success, [class*='notification'][class*='success']";

    /// <summary>Error notification bar</summary>
    public const string ErrorNotification = "div.ms-MessageBar.ms-MessageBar--error, [class*='notification'][class*='error']";

    /// <summary>Any notification bar (success, error, warning)</summary>
    public const string AnyNotification = "div.ms-MessageBar, [data-id='notificationWrapper'] div";

    /// <summary>Notification message text</summary>
    public const string NotificationText = "div.ms-MessageBar-text, span[class*='notification-text']";

    // ─── Form Header ─────────────────────────────────────────────────────────

    /// <summary>The page/form title (e.g. account name in the header)</summary>
    public const string FormTitle = "[data-id='header_title'], h1.ms-Label, [class*='page-header'] h1";

    /// <summary>Business process flow stage container</summary>
    public const string BpfStage = "[data-id='ProcessStageContainer']";
}
