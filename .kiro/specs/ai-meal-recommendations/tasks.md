# Implementation Plan: AI-Powered Meal Recommendations

## Overview

This implementation plan breaks down the AI-powered meal recommendation system into discrete, incremental coding tasks. Each task builds on previous work, with testing integrated throughout to validate functionality early. The implementation follows a bottom-up approach: data layer → services → controllers → integration.

## Tasks

- [x] 1. Create database entities and migrations
  - Create TrainingDataset, AIConfiguration, and AIOperationLog entity classes
  - Add DbSet properties to ApplicationDbContext
  - Create and apply EF Core migration for new tables with indexes
  - _Requirements: 1.1, 5.1, 6.1_

- [x] 1.1 Write property tests for entity validation
  - **Property 4: Invalid dataset rejection**
  - **Validates: Requirements 1.5**

- [ ] 2. Implement Excel Import Service
  - [x] 2.1 Create IExcelImportService interface and ExcelImportService implementation
    - Implement ImportFromExcelAsync method using EPPlus
    - Parse Excel rows into TrainingDataset entities
    - Implement row-level validation and error collection
    - Implement ShouldAutoImportAsync to check for empty database
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  
  - [x] 2.2 Write property test for Excel import validation
    - **Property 5: Excel import row validation**
    - **Validates: Requirements 2.3, 2.4**
  
  - [ ] 2.3 Write property test for import summary accuracy
    - **Property 6: Import summary accuracy**
    - **Validates: Requirements 2.5**
  
  - [ ] 2.4 Write unit tests for Excel import edge cases
    - Test missing file handling
    - Test corrupted file handling
    - Test empty Excel file
    - _Requirements: 7.1_

- [ ] 3. Implement Dataset Management Service
  - [ ] 3.1 Create IDatasetManagementService interface and implementation
    - Implement CRUD operations (GetAllAsync, GetByIdAsync, CreateAsync, UpdateAsync, DeleteAsync)
    - Implement ValidateAsync for training dataset validation
    - Add JSON validation for PreferredMealTypes, CommonAllergies, and RecommendationWeights fields
    - _Requirements: 1.2, 1.3, 1.4, 1.5_
  
  - [ ] 3.2 Write property test for dataset creation persistence
    - **Property 1: Training dataset creation persistence**
    - **Validates: Requirements 1.2**
  
  - [ ] 3.3 Write property test for dataset update persistence
    - **Property 2: Training dataset update persistence**
    - **Validates: Requirements 1.3**
  
  - [ ] 3.4 Write property test for dataset deletion completeness
    - **Property 3: Training dataset deletion completeness**
    - **Validates: Requirements 1.4**

- [ ] 4. Implement AI Operation Logger
  - [ ] 4.1 Create IAIOperationLogger interface and implementation
    - Implement LogOperationAsync for direct logging
    - Implement StartOperationAsync to create log entry and return ID
    - Implement CompleteOperationAsync to update log with success details
    - Implement FailOperationAsync to update log with error details
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_
  
  - [ ] 4.2 Write property test for successful operation logging
    - **Property 18: Successful operation logging**
    - **Validates: Requirements 5.1, 5.2, 5.4, 5.5**
  
  - [ ] 4.3 Write property test for failed operation logging
    - **Property 19: Failed operation logging**
    - **Validates: Requirements 5.3, 5.4, 5.5**
  
  - [ ] 4.4 Write property test for log persistence immediacy
    - **Property 20: Log persistence immediacy**
    - **Validates: Requirements 5.5**

- [ ] 5. Checkpoint - Ensure data layer tests pass
  - Run all tests for entities, Excel import, dataset management, and logging
  - Verify database migrations apply successfully
  - Ask the user if questions arise

- [ ] 6. Implement AI Configuration Service
  - [ ] 6.1 Create IAIConfigurationService interface and implementation
    - Implement GetConfigurationAsync (create default if not exists)
    - Implement UpdateConfigurationAsync with admin tracking
    - Implement GetOperationLogsAsync with filtering and pagination
    - _Requirements: 6.1, 6.2, 6.3, 6.5_
  
  - [ ] 6.2 Write property test for AI enablement effect
    - **Property 21: AI enablement effect**
    - **Validates: Requirements 6.2**
  
  - [ ] 6.3 Write property test for AI disablement effect
    - **Property 22: AI disablement effect**
    - **Validates: Requirements 6.3**
  
  - [ ] 6.4 Write property test for operation log filtering
    - **Property 23: Operation log filtering**
    - **Validates: Requirements 6.5**

- [ ] 7. Implement Customer Profile Analyzer
  - [ ] 7.1 Create ICustomerProfileAnalyzer interface and implementation
    - Implement AnalyzeCustomerAsync to retrieve all customer context data
    - Query HealthProfile, Allergies, FoodPreferences, and Order history
    - Build CustomerContext object with completeness flags
    - Track missing data in MissingDataWarnings list
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_
  
  - [ ] 7.2 Write property test for complete customer context retrieval
    - **Property 7: Complete customer context retrieval**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4**
  
  - [ ] 7.3 Write property test for incomplete profile handling
    - **Property 8: Graceful handling of incomplete profiles**
    - **Validates: Requirements 3.5**

- [ ] 8. Implement Recommendation Engine
  - [ ] 8.1 Create IRecommendationEngine interface and implementation
    - Implement GenerateRecommendationsAsync with scoring algorithm
    - Implement allergy filtering (hard constraint - exclude meals with allergens)
    - Implement dietary restriction matching with scoring
    - Implement preference matching with scoring
    - Implement nutritional balance calculation
    - Implement variety bonus based on order history
    - Implement calorie alignment scoring
    - Sort meals by relevance score and select top N
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 8.1, 8.2, 8.3, 8.4, 8.5_
  
  - [ ] 8.2 Write property test for recommendation count constraints
    - **Property 9: Recommendation count constraints**
    - **Validates: Requirements 4.1**
  
  - [ ] 8.3 Write property test for allergy exclusion (CRITICAL)
    - **Property 10: Allergy exclusion (Critical Safety Property)**
    - **Validates: Requirements 4.2**
  
  - [ ] 8.4 Write property test for dietary restriction prioritization
    - **Property 11: Dietary restriction prioritization**
    - **Validates: Requirements 4.3**
  
  - [ ] 8.5 Write property test for nutritional balance calculation
    - **Property 12: Nutritional balance calculation**
    - **Validates: Requirements 4.4, 8.1, 8.5**
  
  - [ ] 8.6 Write property test for order history variety
    - **Property 13: Order history variety**
    - **Validates: Requirements 4.5**
  
  - [ ] 8.7 Write property test for relevance score ordering
    - **Property 14: Relevance score ordering**
    - **Validates: Requirements 4.6**
  
  - [ ] 8.8 Write property test for calorie goal alignment
    - **Property 15: Calorie goal alignment**
    - **Validates: Requirements 8.2**
  
  - [ ] 8.9 Write property test for macronutrient variety
    - **Property 16: Macronutrient variety**
    - **Validates: Requirements 8.3**
  
  - [ ] 8.10 Write property test for incomplete nutritional data exclusion
    - **Property 17: Incomplete nutritional data exclusion**
    - **Validates: Requirements 8.4**

- [ ] 9. Checkpoint - Ensure recommendation engine tests pass
  - Run all recommendation engine tests
  - Verify allergy exclusion property test passes (critical safety property)
  - Ask the user if questions arise

- [ ] 10. Implement AI Recommendation Service (Orchestrator)
  - [ ] 10.1 Create IAIRecommendationService interface and implementation
    - Implement GenerateRecommendationsAsync orchestration method
    - Check AI configuration status via IAIConfigurationService
    - Use IAIOperationLogger to track operation start/completion
    - Delegate to ICustomerProfileAnalyzer for customer context
    - Delegate to IRecommendationEngine for meal generation
    - Implement retry logic with exponential backoff for transient failures
    - Implement exception handling with user-friendly error messages
    - Implement IsAIEnabledAsync method
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 6.2, 6.3, 7.3, 7.4_
  
  - [ ] 10.2 Write property test for recommendation exception handling
    - **Property 24: Recommendation exception handling**
    - **Validates: Requirements 7.3**
  
  - [ ] 10.3 Write property test for transient failure retry
    - **Property 25: Transient failure retry**
    - **Validates: Requirements 7.4**
  
  - [ ] 10.4 Write property test for critical error severity logging
    - **Property 26: Critical error severity logging**
    - **Validates: Requirements 7.5**
  
  - [ ] 10.5 Write unit tests for AI service integration
    - Test complete recommendation workflow
    - Test disabled AI state handling
    - Test incomplete customer profile handling
    - _Requirements: 3.5, 6.4_

- [ ] 11. Register services in dependency injection container
  - Add all service registrations to Program.cs or Startup.cs
  - Register IExcelImportService, IDatasetManagementService, IAIOperationLogger
  - Register IAIConfigurationService, ICustomerProfileAnalyzer, IRecommendationEngine
  - Register IAIRecommendationService
  - Configure service lifetimes (Scoped for database-dependent services)
  - _Requirements: All_

- [ ] 12. Create Admin Dataset Management Controller and Views
  - [ ] 12.1 Create DatasetManagementController with CRUD actions
    - Implement Index action to display all datasets
    - Implement Create GET/POST actions with validation
    - Implement Edit GET/POST actions with validation
    - Implement Delete GET/POST actions
    - Implement Import action to trigger manual Excel import
    - Add authorization attributes for admin-only access
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.2, 2.5_
  
  - [ ] 12.2 Create Razor views for dataset management
    - Create Index.cshtml with dataset table and action buttons
    - Create Create.cshtml with form and validation
    - Create Edit.cshtml with form and validation
    - Create Delete.cshtml with confirmation
    - Display import summary after manual import
    - _Requirements: 1.1, 2.5_
  
  - [ ] 12.3 Write unit tests for dataset controller actions
    - Test Index returns all datasets
    - Test Create with valid/invalid data
    - Test Edit with valid/invalid data
    - Test Delete removes dataset
    - Test Import triggers Excel import
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [ ] 13. Create Admin AI Configuration Controller and Views
  - [ ] 13.1 Create AIConfigurationController with configuration actions
    - Implement Index action to display current configuration
    - Implement Edit POST action to update configuration
    - Implement OperationLogs action with filtering and pagination
    - Add authorization attributes for admin-only access
    - _Requirements: 6.1, 6.2, 6.3, 6.5_
  
  - [ ] 13.2 Create Razor views for AI configuration
    - Create Index.cshtml with configuration form and enable/disable toggle
    - Create OperationLogs.cshtml with filterable log table and pagination
    - Display current AI status prominently
    - _Requirements: 6.1, 6.5_
  
  - [ ] 13.3 Write unit tests for AI configuration controller
    - Test Index displays configuration
    - Test Edit updates configuration
    - Test OperationLogs returns filtered logs
    - _Requirements: 6.1, 6.2, 6.3, 6.5_

- [ ] 14. Create Customer Recommendations API endpoint
  - [ ] 14.1 Create RecommendationsController or add to existing customer controller
    - Implement GetRecommendations action (GET /api/recommendations or similar)
    - Get current customer ID from authentication context
    - Call IAIRecommendationService.GenerateRecommendationsAsync
    - Return RecommendationResult as JSON
    - Handle disabled AI state with appropriate HTTP status and message
    - Handle errors with appropriate HTTP status codes
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 6.4, 7.3_
  
  - [ ] 14.2 Write unit tests for recommendations API endpoint
    - Test successful recommendation generation
    - Test disabled AI returns appropriate error
    - Test customer not found returns 404
    - Test exception handling returns 500 with user-friendly message
    - _Requirements: 6.4, 7.3_

- [ ] 15. Implement application startup logic for auto-import
  - [ ] 15.1 Add startup initialization in Program.cs
    - Check if auto-import should run via IExcelImportService.ShouldAutoImportAsync
    - Trigger ImportFromExcelAsync if needed
    - Log import results
    - Ensure application continues even if import fails
    - _Requirements: 2.1, 7.1_
  
  - [ ] 15.2 Write integration test for auto-import on first run
    - Test empty database triggers auto-import
    - Test populated database skips auto-import
    - Test missing Excel file doesn't prevent startup
    - _Requirements: 2.1, 7.1_

- [ ] 16. Create sample Excel file and seed data
  - Create files/PRN222_Datasets.xlsx with sample training data
  - Include at least 5 diverse customer segments
  - Include valid JSON for PreferredMealTypes, CommonAllergies, and RecommendationWeights
  - Document Excel file structure in README or comments
  - _Requirements: 2.1, 2.2_

- [ ] 17. Final checkpoint - Integration testing and validation
  - Run complete test suite (unit tests and property tests)
  - Verify all 26 correctness properties pass
  - Test complete admin workflow: import → manage datasets → configure AI → view logs
  - Test complete customer workflow: profile setup → generate recommendations
  - Test error scenarios: disabled AI, missing data, invalid input
  - Verify database migrations and indexes
  - Ask the user if questions arise

- [ ] 18. Add navigation links and UI polish
  - Add navigation menu items for Dataset Management and AI Configuration (admin only)
  - Add customer-facing UI for viewing recommendations (if not already present)
  - Ensure consistent styling with existing application
  - Add loading indicators for recommendation generation
  - Add user-friendly error messages throughout
  - _Requirements: 1.1, 6.1_

## Notes

- All tasks are required for comprehensive implementation with full test coverage
- Property tests are critical for validating business rules, especially Property 10 (allergy exclusion)
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at key milestones
- The implementation uses rule-based AI logic initially, with architecture supporting future ML.NET integration
- All services use dependency injection for testability and maintainability
- Retry logic and error handling are built into the AI Recommendation Service for resilience
