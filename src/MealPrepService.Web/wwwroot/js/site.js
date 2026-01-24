// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Initialize Select2 on all select elements for searchable dropdowns
$(document).ready(function() {
    // Function to initialize Select2
    function initializeSelect2(element) {
        var $element = $(element);
        
        // Check if Select2 is already initialized
        if ($element.hasClass('select2-hidden-accessible')) {
            return; // Already initialized, skip
        }
        
        // Get placeholder from data attribute or first option text
        var placeholder = $element.data('placeholder');
        if (!placeholder) {
            var firstOption = $element.find('option:first');
            placeholder = firstOption.text() || 'Select an option...';
        }
        
        $element.select2({
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: placeholder,
            allowClear: false, // Changed to false to ensure placeholder shows
            dropdownAutoWidth: false,
            minimumResultsForSearch: 0, // Always show search box
            dropdownParent: $element.parent(),
            language: {
                noResults: function() {
                    return "No results found";
                },
                searching: function() {
                    return "Searching...";
                }
            },
            templateResult: function(data) {
                return data.text;
            },
            templateSelection: function(data) {
                // Show placeholder if no selection or selecting the placeholder option
                if (!data.id || data.id === '') {
                    return $('<span class="select2-selection__placeholder">' + placeholder + '</span>');
                }
                return data.text;
            }
        });
        
        // Clean up on dropdown open
        $element.on('select2:open', function() {
            // Hide any sibling hint text
            $(this).parent().find('.form-text, small').hide();
            
            // Focus on search input
            setTimeout(function() {
                $('.select2-search--dropdown .select2-search__field').focus();
            }, 100);
        });
        
        // Show hint text again on close
        $element.on('select2:close', function() {
            $(this).parent().find('.form-text, small').show();
        });
    }
    
    // Initialize Select2 on all existing select elements
    $('select').each(function() {
        initializeSelect2(this);
    });
    
    // Handle dynamically added select elements (using MutationObserver instead of deprecated DOMNodeInserted)
    var observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.addedNodes && mutation.addedNodes.length > 0) {
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === 1) { // Element node
                        if (node.tagName === 'SELECT') {
                            initializeSelect2(node);
                        }
                        // Check for select elements within added nodes
                        $(node).find('select').each(function() {
                            initializeSelect2(this);
                        });
                    }
                });
            }
        });
    });
    
    // Start observing the document body for changes
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
});

