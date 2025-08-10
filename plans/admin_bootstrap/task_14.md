# Task 14: Implement Entity Create/Edit Forms

## Objective
Create comprehensive forms for creating and editing all entities (Events, Users, Invitations, Reservations) with HTMX-powered submissions, client and server-side validation, error handling, and success notifications.

## Requirements
- Implement create and edit forms for all entity types
- Use HTMX for form submissions without page refresh
- Add comprehensive validation (client and server)
- Implement error handling with user-friendly messages
- Add success notifications and redirects
- Support draft saving and form persistence
- Ensure accessibility standards are met

## Implementation Steps

### Step 1: Create Form Controllers
Location: `/SocialAnimal.Web/Areas/Admin/Controllers/`

Add create/edit actions to controllers:
```csharp
public class EventsController : AdminControllerBase
{
    // GET: Create form
    [HttpGet]
    public IActionResult Create()
    {
        var model = new EventFormViewModel
        {
            Event = new EventFormModel(),
            States = _locationService.GetStates(),
            IsNew = true
        };
        return View("Form", model);
    }
    
    // POST: Create submission
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormModel model)
    {
        if (!ModelState.IsValid)
        {
            if (IsHtmxRequest())
                return PartialView("_FormErrors", ModelState);
            
            return View("Form", new EventFormViewModel 
            { 
                Event = model,
                States = _locationService.GetStates(),
                IsNew = true
            });
        }
        
        try
        {
            var eventStub = _mapper.Map<EventStub>(model);
            var created = await _eventService.CreateEventAsync(eventStub);
            
            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = $"/admin/events/{created.Slug}";
                return PartialView("_SuccessMessage", "Event created successfully!");
            }
            
            TempData["Success"] = "Event created successfully!";
            return RedirectToAction("Details", new { slug = created.Slug });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            
            if (IsHtmxRequest())
                return PartialView("_FormErrors", ModelState);
                
            return View("Form", new EventFormViewModel 
            { 
                Event = model,
                States = _locationService.GetStates(),
                IsNew = true
            });
        }
    }
    
    // GET: Edit form
    [HttpGet]
    public async Task<IActionResult> Edit(string slug)
    {
        var eventRecord = await _eventService.GetBySlugAsync(slug);
        if (eventRecord == null)
            return NotFound();
            
        var model = new EventFormViewModel
        {
            Event = _mapper.Map<EventFormModel>(eventRecord),
            States = _locationService.GetStates(),
            IsNew = false
        };
        
        return View("Form", model);
    }
    
    // POST: Edit submission
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string slug, EventFormModel model)
    {
        // Similar to Create but calls UpdateEventAsync
    }
    
    // Auto-save draft
    [HttpPost]
    public async Task<IActionResult> AutoSave(EventFormModel model)
    {
        var draft = await _draftService.SaveDraftAsync("Event", model);
        return Json(new { saved = true, draftId = draft.Id });
    }
    
    // Validate field
    [HttpPost]
    public IActionResult ValidateField(string field, string value)
    {
        var errors = _validationService.ValidateField<EventFormModel>(field, value);
        return Json(new { valid = !errors.Any(), errors });
    }
}
```

### Step 2: Create Form View Models
Location: `/SocialAnimal.Web/Areas/Admin/Models/FormModels/`

Define form models with validation:
```csharp
public class EventFormModel
{
    public long Id { get; set; }
    
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title must be less than 200 characters")]
    public string Title { get; set; }
    
    [Required(ErrorMessage = "Event date is required")]
    [FutureDate(ErrorMessage = "Event date must be in the future")]
    public DateTime? EventDate { get; set; }
    
    [Required(ErrorMessage = "Address is required")]
    [StringLength(200)]
    public string AddressLine1 { get; set; }
    
    [StringLength(200)]
    public string AddressLine2 { get; set; }
    
    [Required(ErrorMessage = "City is required")]
    [StringLength(100)]
    public string City { get; set; }
    
    [Required(ErrorMessage = "State is required")]
    [RegularExpression("^[A-Z]{2}$", ErrorMessage = "State must be 2-letter code")]
    public string State { get; set; }
    
    [Required(ErrorMessage = "Postal code is required")]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid postal code")]
    public string Postal { get; set; }
    
    [StringLength(500)]
    public string Description { get; set; }
    
    // Metadata
    public DateTime? ConcurrencyToken { get; set; }
}

public class EventFormViewModel
{
    public EventFormModel Event { get; set; }
    public SelectList States { get; set; }
    public bool IsNew { get; set; }
    public Dictionary<string, string> ValidationRules { get; set; }
}
```

### Step 3: Create Event Form View
Location: `/SocialAnimal.Web/Areas/Admin/Views/Events/Form.cshtml`

Implement comprehensive form:
```html
@model EventFormViewModel
@{
    ViewData["Title"] = Model.IsNew ? "Create Event" : "Edit Event";
}

<div class="form-page">
    <!-- Form Header -->
    <div class="form-header">
        <div class="breadcrumb">
            <a href="/admin">Dashboard</a> /
            <a href="/admin/events">Events</a> /
            <span>@ViewData["Title"]</span>
        </div>
        
        <h1>@ViewData["Title"]</h1>
    </div>
    
    <!-- Form Container -->
    <div class="form-container">
        <form id="event-form"
              hx-post="@(Model.IsNew ? "/admin/events/create" : $"/admin/events/{Model.Event.Id}/edit")"
              hx-target="#form-result"
              hx-swap="innerHTML"
              hx-indicator="#form-loading">
              
            @Html.AntiForgeryToken()
            
            <!-- Hidden fields -->
            @if(!Model.IsNew)
            {
                <input type="hidden" asp-for="Event.Id" />
                <input type="hidden" asp-for="Event.ConcurrencyToken" />
            }
            
            <!-- Form Errors Container -->
            <div id="form-result"></div>
            
            <!-- Event Information Section -->
            <div class="form-section">
                <h2 class="form-section-title">Event Information</h2>
                
                <div class="form-group">
                    <label asp-for="Event.Title" class="required">
                        Event Title
                    </label>
                    <input asp-for="Event.Title" 
                           class="form-control"
                           placeholder="Summer BBQ 2024"
                           hx-post="/admin/events/validate-field"
                           hx-trigger="blur"
                           hx-vals='{"field": "Title"}'
                           hx-target="next .field-error"
                           autofocus />
                    <span asp-validation-for="Event.Title" class="field-error"></span>
                    <small class="form-text">
                        A descriptive title for your event
                    </small>
                </div>
                
                <div class="form-group">
                    <label asp-for="Event.EventDate" class="required">
                        Event Date & Time
                    </label>
                    <input asp-for="Event.EventDate" 
                           type="datetime-local"
                           class="form-control"
                           hx-post="/admin/events/validate-field"
                           hx-trigger="change"
                           hx-vals='{"field": "EventDate"}'
                           hx-target="next .field-error" />
                    <span asp-validation-for="Event.EventDate" class="field-error"></span>
                </div>
                
                <div class="form-group">
                    <label asp-for="Event.Description">
                        Description
                    </label>
                    <textarea asp-for="Event.Description" 
                              class="form-control"
                              rows="4"
                              placeholder="Optional event description..."></textarea>
                    <span asp-validation-for="Event.Description" class="field-error"></span>
                    <small class="form-text">
                        <span class="char-count" data-target="Event.Description">0</span>/500 characters
                    </small>
                </div>
            </div>
            
            <!-- Location Section -->
            <div class="form-section">
                <h2 class="form-section-title">Location</h2>
                
                <div class="form-group">
                    <label asp-for="Event.AddressLine1" class="required">
                        Address Line 1
                    </label>
                    <input asp-for="Event.AddressLine1" 
                           class="form-control"
                           placeholder="123 Main Street" />
                    <span asp-validation-for="Event.AddressLine1" class="field-error"></span>
                </div>
                
                <div class="form-group">
                    <label asp-for="Event.AddressLine2">
                        Address Line 2
                    </label>
                    <input asp-for="Event.AddressLine2" 
                           class="form-control"
                           placeholder="Apt, Suite, Floor (optional)" />
                </div>
                
                <div class="form-row">
                    <div class="form-group col-md-6">
                        <label asp-for="Event.City" class="required">
                            City
                        </label>
                        <input asp-for="Event.City" 
                               class="form-control"
                               placeholder="Seattle" />
                        <span asp-validation-for="Event.City" class="field-error"></span>
                    </div>
                    
                    <div class="form-group col-md-3">
                        <label asp-for="Event.State" class="required">
                            State
                        </label>
                        <select asp-for="Event.State" 
                                asp-items="Model.States"
                                class="form-control">
                            <option value="">Select State</option>
                        </select>
                        <span asp-validation-for="Event.State" class="field-error"></span>
                    </div>
                    
                    <div class="form-group col-md-3">
                        <label asp-for="Event.Postal" class="required">
                            Postal Code
                        </label>
                        <input asp-for="Event.Postal" 
                               class="form-control"
                               placeholder="98101"
                               pattern="^\d{5}(-\d{4})?$" />
                        <span asp-validation-for="Event.Postal" class="field-error"></span>
                    </div>
                </div>
                
                <!-- Map Preview -->
                <div class="map-preview-container">
                    <div id="map-preview" class="map-preview">
                        <!-- Map will be rendered here -->
                    </div>
                    <button type="button" 
                            class="btn btn-link"
                            onclick="validateAddress()">
                        Validate Address
                    </button>
                </div>
            </div>
            
            <!-- Form Actions -->
            <div class="form-actions">
                <div class="form-actions-left">
                    <button type="submit" class="btn btn-primary">
                        <i class="icon-save"></i>
                        @(Model.IsNew ? "Create Event" : "Save Changes")
                    </button>
                    
                    @if(Model.IsNew)
                    {
                        <button type="button" 
                                class="btn btn-secondary"
                                hx-post="/admin/events/create-and-continue"
                                hx-include="#event-form">
                            Save & Add Another
                        </button>
                    }
                    
                    <a href="@(Model.IsNew ? "/admin/events" : $"/admin/events/{Model.Event.Id}")" 
                       class="btn btn-link">
                        Cancel
                    </a>
                </div>
                
                <div class="form-actions-right">
                    <span class="auto-save-status" id="auto-save-status">
                        <i class="icon-check"></i> Draft saved
                    </span>
                </div>
            </div>
            
            <!-- Loading Indicator -->
            <div id="form-loading" class="form-loading" style="display:none;">
                <div class="spinner"></div>
                Saving...
            </div>
        </form>
    </div>
</div>
```

### Step 4: Create User Form
Location: `/SocialAnimal.Web/Areas/Admin/Views/Users/Form.cshtml`

User-specific form fields:
- Name (required)
- Phone number (required, with formatting)
- Slug (auto-generated or custom)
- Notes/Bio (optional)

Include phone validation and formatting:
```html
<div class="form-group">
    <label asp-for="User.Phone" class="required">
        Phone Number
    </label>
    <input asp-for="User.Phone" 
           type="tel"
           class="form-control"
           placeholder="(555) 123-4567"
           data-mask="(000) 000-0000"
           hx-post="/admin/users/validate-phone"
           hx-trigger="blur"
           hx-target="#phone-validation" />
    <span id="phone-validation" class="field-error"></span>
    <small class="form-text">
        10-digit US phone number
    </small>
</div>
```

### Step 5: Create Invitation Form
Location: `/SocialAnimal.Web/Areas/Admin/Views/Invitations/Form.cshtml`

Invitation-specific fields:
- Event selection (dropdown or autocomplete)
- Guest name (required)
- Email address (conditional)
- Phone number (conditional)
- Max party size (default from settings)
- Custom message (optional)
- Send immediately checkbox

Implement conditional validation:
```javascript
// Require either email or phone
function validateContact() {
    const email = document.getElementById('Email').value;
    const phone = document.getElementById('Phone').value;
    
    if (!email && !phone) {
        showError('Either email or phone is required');
        return false;
    }
    return true;
}
```

### Step 6: Create Reservation Form
Location: `/SocialAnimal.Web/Areas/Admin/Views/Reservations/Form.cshtml`

Reservation-specific fields:
- Invitation selection (autocomplete)
- Party size (0 for regrets)
- Attendance status (attending/regrets)
- Dietary restrictions (optional)
- Notes (optional)
- User association (optional, autocomplete)

Dynamic party size based on status:
```javascript
document.getElementById('Status').addEventListener('change', function() {
    const partySize = document.getElementById('PartySize');
    if (this.value === 'regrets') {
        partySize.value = 0;
        partySize.disabled = true;
    } else {
        partySize.disabled = false;
    }
});
```

### Step 7: Create Form Validation Service
Location: `/SocialAnimal.Infrastructure/Services/ValidationService.cs`

Implement validation logic:
```csharp
public class ValidationService : IValidationService
{
    public async Task<ValidationResult> ValidateEventAsync(EventStub stub)
    {
        var errors = new List<ValidationError>();
        
        // Check slug uniqueness
        if (await _eventRepo.SlugExistsAsync(stub.Slug))
        {
            errors.Add(new ValidationError("Slug", "This URL slug is already in use"));
        }
        
        // Validate event date
        if (stub.EventDate <= DateTime.Now)
        {
            errors.Add(new ValidationError("EventDate", "Event date must be in the future"));
        }
        
        // Validate address
        if (!IsValidPostalCode(stub.Postal, stub.State))
        {
            errors.Add(new ValidationError("Postal", "Invalid postal code for state"));
        }
        
        return new ValidationResult(errors);
    }
    
    public Dictionary<string, string> GetClientValidationRules<T>()
    {
        // Return validation rules for client-side validation
        var rules = new Dictionary<string, string>();
        var properties = typeof(T).GetProperties();
        
        foreach (var prop in properties)
        {
            var attributes = prop.GetCustomAttributes();
            // Build validation rules from attributes
        }
        
        return rules;
    }
}
```

### Step 8: Create Form JavaScript
Location: `/SocialAnimal.Web/wwwroot/js/admin/forms.js`

Implement form enhancements:
```javascript
// Auto-save functionality
let autoSaveTimer;
const AUTOSAVE_DELAY = 30000; // 30 seconds

document.addEventListener('DOMContentLoaded', function() {
    initializeForm();
    initializeAutosave();
    initializeValidation();
    initializeFieldEnhancements();
});

function initializeForm() {
    // Add form change tracking
    const form = document.getElementById('event-form');
    let formChanged = false;
    
    form.addEventListener('change', function() {
        formChanged = true;
        startAutosave();
    });
    
    // Warn on navigation if unsaved changes
    window.addEventListener('beforeunload', function(e) {
        if (formChanged) {
            e.preventDefault();
            e.returnValue = 'You have unsaved changes. Are you sure you want to leave?';
        }
    });
    
    // Clear warning on successful submit
    form.addEventListener('htmx:afterRequest', function(evt) {
        if (evt.detail.successful) {
            formChanged = false;
        }
    });
}

function initializeAutosave() {
    function startAutosave() {
        clearTimeout(autoSaveTimer);
        autoSaveTimer = setTimeout(autosave, AUTOSAVE_DELAY);
    }
    
    function autosave() {
        const form = document.getElementById('event-form');
        const formData = new FormData(form);
        
        fetch('/admin/events/auto-save', {
            method: 'POST',
            body: formData
        })
        .then(response => response.json())
        .then(data => {
            if (data.saved) {
                showAutoSaveStatus('Draft saved');
            }
        })
        .catch(error => {
            console.error('Autosave failed:', error);
        });
    }
}

function initializeValidation() {
    // Real-time validation
    document.querySelectorAll('[data-validate]').forEach(input => {
        input.addEventListener('blur', function() {
            validateField(this);
        });
    });
    
    // Character counters
    document.querySelectorAll('[maxlength]').forEach(input => {
        const counter = document.querySelector(`[data-target="${input.name}"]`);
        if (counter) {
            input.addEventListener('input', function() {
                counter.textContent = this.value.length;
            });
        }
    });
}

function initializeFieldEnhancements() {
    // Slug generation
    const titleInput = document.getElementById('Title');
    const slugInput = document.getElementById('Slug');
    
    if (titleInput && slugInput && !slugInput.value) {
        titleInput.addEventListener('blur', function() {
            if (!slugInput.value) {
                slugInput.value = generateSlug(this.value);
            }
        });
    }
    
    // Phone formatting
    document.querySelectorAll('[data-mask]').forEach(input => {
        const mask = input.dataset.mask;
        input.addEventListener('input', function(e) {
            this.value = formatWithMask(this.value, mask);
        });
    });
    
    // Date/time enhancement
    flatpickr('.datetime-picker', {
        enableTime: true,
        dateFormat: 'Y-m-d H:i',
        minDate: 'today'
    });
}

function generateSlug(text) {
    return text
        .toLowerCase()
        .replace(/[^\w\s-]/g, '')
        .replace(/\s+/g, '-')
        .replace(/-+/g, '-')
        .trim();
}

function formatWithMask(value, mask) {
    // Implement mask formatting
    const digits = value.replace(/\D/g, '');
    let formatted = '';
    let digitIndex = 0;
    
    for (let i = 0; i < mask.length && digitIndex < digits.length; i++) {
        if (mask[i] === '0') {
            formatted += digits[digitIndex++];
        } else {
            formatted += mask[i];
        }
    }
    
    return formatted;
}

function showAutoSaveStatus(message) {
    const status = document.getElementById('auto-save-status');
    status.style.display = 'block';
    status.textContent = message;
    
    setTimeout(() => {
        status.style.display = 'none';
    }, 3000);
}
```

### Step 9: Create Form Styling
Location: `/SocialAnimal.Web/wwwroot/css/admin/forms.css`

Style the forms:
```css
.form-page {
    max-width: 800px;
    margin: 0 auto;
    padding: var(--spacing-lg);
}

.form-container {
    background: white;
    border-radius: var(--border-radius);
    box-shadow: var(--shadow-md);
    padding: var(--spacing-xl);
}

.form-section {
    margin-bottom: var(--spacing-xl);
    padding-bottom: var(--spacing-xl);
    border-bottom: 1px solid var(--color-gray-200);
}

.form-section:last-child {
    border-bottom: none;
}

.form-section-title {
    font-size: 1.25rem;
    font-weight: 600;
    margin-bottom: var(--spacing-lg);
    color: var(--color-gray-800);
}

.form-group {
    margin-bottom: var(--spacing-lg);
}

.form-group label {
    display: block;
    font-weight: 500;
    margin-bottom: var(--spacing-xs);
    color: var(--color-gray-700);
}

.form-group label.required::after {
    content: " *";
    color: var(--color-danger);
}

.form-control {
    width: 100%;
    padding: var(--spacing-sm) var(--spacing-md);
    border: 1px solid var(--color-gray-300);
    border-radius: var(--border-radius-sm);
    font-size: 1rem;
    transition: border-color 0.2s, box-shadow 0.2s;
}

.form-control:focus {
    outline: none;
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px rgba(var(--color-primary-rgb), 0.1);
}

.form-control.is-invalid {
    border-color: var(--color-danger);
}

.field-error {
    display: block;
    color: var(--color-danger);
    font-size: 0.875rem;
    margin-top: var(--spacing-xs);
}

.form-text {
    display: block;
    color: var(--color-gray-600);
    font-size: 0.875rem;
    margin-top: var(--spacing-xs);
}

.form-row {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: var(--spacing-md);
}

.form-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: var(--spacing-xl);
    padding-top: var(--spacing-xl);
    border-top: 1px solid var(--color-gray-200);
}

.form-actions-left {
    display: flex;
    gap: var(--spacing-md);
    align-items: center;
}

.auto-save-status {
    display: none;
    color: var(--color-success);
    font-size: 0.875rem;
}

.form-loading {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: white;
    padding: var(--spacing-lg);
    border-radius: var(--border-radius);
    box-shadow: var(--shadow-lg);
    z-index: 9999;
}

/* Responsive */
@media (max-width: 768px) {
    .form-container {
        padding: var(--spacing-md);
    }
    
    .form-actions {
        flex-direction: column;
        align-items: stretch;
        gap: var(--spacing-md);
    }
    
    .form-actions-left {
        flex-direction: column;
    }
}
```

### Step 10: Create Success/Error Partials
Location: `/SocialAnimal.Web/Areas/Admin/Views/Shared/`

Create feedback partials:

`_FormErrors.cshtml`:
```html
@model ModelStateDictionary

@if(!ViewData.ModelState.IsValid)
{
    <div class="alert alert-danger" role="alert">
        <h4 class="alert-heading">Please correct the following errors:</h4>
        <ul class="mb-0">
            @foreach(var error in ViewData.ModelState.Values.SelectMany(v => v.Errors))
            {
                <li>@error.ErrorMessage</li>
            }
        </ul>
    </div>
}
```

`_SuccessMessage.cshtml`:
```html
@model string

<div class="alert alert-success alert-dismissible" role="alert">
    <i class="icon-check-circle"></i>
    @Model
    <button type="button" class="close" data-dismiss="alert">
        <span>&times;</span>
    </button>
</div>
```

### Step 11: Implement Bulk Create Forms
Location: `/SocialAnimal.Web/Areas/Admin/Views/Invitations/BulkCreate.cshtml`

Create bulk invitation form:
```html
<div class="bulk-form">
    <h2>Bulk Create Invitations</h2>
    
    <div class="bulk-options">
        <button onclick="addRow()">Add Row</button>
        <button onclick="importCSV()">Import CSV</button>
        <button onclick="clearAll()">Clear All</button>
    </div>
    
    <table class="bulk-table" id="bulk-invitations">
        <thead>
            <tr>
                <th>Name *</th>
                <th>Email</th>
                <th>Phone</th>
                <th>Max Party</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            <tr class="bulk-row">
                <td><input type="text" name="invitations[0].Name" required></td>
                <td><input type="email" name="invitations[0].Email"></td>
                <td><input type="tel" name="invitations[0].Phone"></td>
                <td><input type="number" name="invitations[0].MaxPartySize" value="2"></td>
                <td><button onclick="removeRow(this)">×</button></td>
            </tr>
        </tbody>
    </table>
    
    <div class="form-actions">
        <button type="submit" class="btn btn-primary">
            Create All Invitations
        </button>
    </div>
</div>
```

## Testing Checklist

- [ ] All forms load with correct initial data
- [ ] Validation works on client and server
- [ ] Error messages display correctly
- [ ] Success messages and redirects work
- [ ] HTMX submissions work without refresh
- [ ] Auto-save functions properly
- [ ] Field formatting works correctly
- [ ] Conditional fields show/hide properly
- [ ] File uploads work (if applicable)
- [ ] Bulk operations execute successfully
- [ ] Form state persists on validation errors
- [ ] Accessibility standards met

## Validation Requirements

1. **Client-side Validation**:
   - HTML5 validation attributes
   - Custom JavaScript validation
   - Real-time field validation
   - Clear error messages

2. **Server-side Validation**:
   - Model validation attributes
   - Business rule validation
   - Duplicate checking
   - Concurrency handling

## Accessibility Requirements

1. **Form Structure**: Proper fieldsets and legends
2. **Labels**: Associated with inputs
3. **Required Fields**: Clear indication
4. **Error Messages**: Associated with fields
5. **Keyboard Navigation**: Tab order correct
6. **Screen Readers**: ARIA labels where needed

## Dependencies

This task depends on:
- Task 8: Services for business logic
- Task 9: MVC infrastructure
- Task 10: Layout for form pages

This task must be completed before:
- Task 15: Controllers need form handling

## Notes

- Consider implementing form wizards for complex flows
- Add field dependencies and conditional logic
- Support file uploads where needed
- Implement progress indicators for long forms
- Add form templates/presets
- Consider adding form versioning
- Implement undo/redo functionality
- Add keyboard shortcuts
- Support form duplication
- Consider adding approval workflows
- Implement field-level permissions
- Add support for custom fields
- Consider implementing form analytics
- Support form embedding
- Add A/B testing capability