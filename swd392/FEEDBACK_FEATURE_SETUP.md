# Feedback Feature Implementation Guide

## Overview
A complete feedback system has been implemented with database schema, backend APIs, and frontend UI components. This document provides setup instructions and implementation details.

## ✅ Completed Implementation

### Backend (All Files Created)
1. **Domain Entity**: `src/MealPreparationService.Domain/Entities/Feedback.cs`
   - Properties: Id, CustomerId (FK), Title, Content, CreatedAt, UpdatedAt
   - Relationship: One-to-many with Account (Customer)

2. **Repository Layer**:
   - Interface: `src/MealPreparationService.DataAccess/Repositories/IFeedbackRepository.cs`
   - Implementation: `src/MealPreparationService.DataAccess/Repositories/FeedbackRepository.cs`
   - Methods: GetByCustomerIdAsync, GetAllFeedbacksAsync (paginated)

3. **Business Logic**:
   - Interface: `src/MealPreparationService.Business/Services/IFeedbackService.cs`
   - Implementation: `src/MealPreparationService.Business/Services/FeedbackService.cs`
   - Features: Validation, customer existence checks, pagination, DTO mapping, logging

4. **DTOs**: `src/MealPreparationService.Business/DTOs/FeedbackDtos.cs`
   - CreateFeedbackDto: Title (5-200 chars), Content (10-5000 chars)
   - FeedbackDto: Complete feedback with customer name
   - FeedbackListDto: Paginated feedback list response

5. **API Controller**: `src/MealPreparationService.API/Controllers/FeedbackController.cs`
   - POST `/api/feedbacks` - Create feedback (Customer only)
   - GET `/api/feedbacks/{id}` - Get feedback by ID (role-based access)
   - GET `/api/feedbacks/my-feedbacks/list` - Customer's feedbacks (Customer only)
   - GET `/api/feedbacks/all` - All feedbacks paginated (Manager/Admin only)

6. **Database Configuration**:
   - Updated: `ApplicationDbContext.cs` - Added DbSet and fluent configuration
   - Updated: `Account.cs` - Added Feedbacks navigation property
   - Updated: `IUnitOfWork.cs` & `UnitOfWork.cs` - Added repository registration
   - Created: Migration file with proper constraints and indexes

7. **Dependency Injection**:
   - Updated: `Program.cs` - Registered IFeedbackRepository and IFeedbackService

### Frontend (All Files Created)

1. **Main Page**: `frontend/src/pages/Feedback.tsx`
   - Role-based conditional rendering (Customer vs Manager/Admin views)
   - Fetch feedbacks on page load and pagination change
   - Error handling and loading states
   - Modal for creating feedback (customer only)

2. **Components**:
   - `frontend/src/pages/components/CreateFeedbackModal.tsx` - Feedback submission form with validation
   - `frontend/src/pages/components/FeedbackList.tsx` - Customer's feedback list display
   - `frontend/src/pages/components/AdminFeedbackList.tsx` - Paginated admin feedback table

3. **Styling**: `frontend/src/pages/Feedback.css`
   - Modal styling with backdrop overlay
   - Form input styles with character counters
   - Feedback list and table styles
   - Pagination controls
   - Responsive design for mobile devices
   - Loading spinner and error states

4. **API Service**: `frontend/src/services/feedbackService.ts`
   - createFeedback(data) - POST new feedback
   - getMyFeedbacks() - GET customer's feedbacks
   - getFeedbackById(feedbackId) - GET single feedback
   - getAllFeedbacks(page, pageSize) - GET paginated feedback list

5. **TypeScript Types**: `frontend/src/types/feedback.ts`
   - CreateFeedbackDto interface
   - FeedbackDto interface
   - FeedbackListDto interface

6. **Routing**: 
   - Updated: `frontend/src/App.tsx` - Added `/feedback` protected route
   - Updated: `frontend/src/components/layout/Header.tsx` - Added Feedback link in user dropdown menu

## 🔧 Setup Instructions

### Step 1: Apply Database Migration
```bash
cd src/MealPreparationService.DataAccess
dotnet ef database update
```

This will create the `Feedbacks` table with proper constraints:
- Primary key on Id
- Foreign key to Accounts table with cascade delete
- Index on CustomerId for query performance

### Step 2: Verify Backend Compilation
```bash
cd src/MealPreparationService.API
dotnet build
```

Ensure no compilation errors. All dependencies are already registered in Program.cs.

### Step 3: Frontend is Ready
All frontend components are created and routed. No additional configuration needed.
- The Feedback page is accessible at `/feedback`
- The Feedback link appears in the user dropdown menu for all authenticated users
- Functionality is gated by user role at the UI and API levels

## 📋 Features

### Customer Features
- ✅ Create feedback with title and content
- ✅ View their own feedbacks (ordered by creation date, newest first)
- ✅ See feedback creation date and time
- ✅ Form validation (character limits, required fields)
- ✅ Error handling and success feedback

### Manager/Admin Features
- ✅ View all feedbacks from all customers
- ✅ Paginated view (items per page configurable, max 100)
- ✅ See customer name for each feedback
- ✅ View feedback content with preview
- ✅ Quick view button for full feedback content

## 🔐 Authorization

- **Customer**: Can create their own feedback and view only their own feedbacks
- **Manager/Admin**: Can view all feedbacks with pagination
- **Role-based access control** at both API and UI level

## 🗄️ Database Schema

### Feedbacks Table
```sql
CREATE TABLE [Feedbacks] (
    [Id] nvarchar(450) PRIMARY KEY,
    [CustomerId] nvarchar(450) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    FOREIGN KEY ([CustomerId]) REFERENCES [Accounts]([Id]) ON DELETE CASCADE,
    INDEX [IX_Feedbacks_CustomerId] ON ([CustomerId])
)
```

## 📱 API Endpoints

### Create Feedback
```
POST /api/feedbacks
Authorization: Bearer {token}
Role: Customer
Body: { "title": "string", "content": "string" }
Response: { "id": "string", "customerId": "string", ... }
```

### Get My Feedbacks
```
GET /api/feedbacks/my-feedbacks/list
Authorization: Bearer {token}
Role: Customer
Response: [ { "id": "string", ... } ]
```

### Get Feedback by ID
```
GET /api/feedbacks/{feedbackId}
Authorization: Bearer {token}
Response: { "id": "string", ... }
```

### Get All Feedbacks (Paginated)
```
GET /api/feedbacks/all?page=1&pageSize=10
Authorization: Bearer {token}
Role: Manager, Admin
Response: { "feedbacks": [...], "total": number, "page": number, "pageSize": number }
```

## 🧪 Testing

### Backend
1. Run the API: `dotnet run` in `src/MealPreparationService.API`
2. Use Swagger UI or Postman to test endpoints at `https://localhost:5001/swagger`
3. Authenticate with a token from login endpoint first

### Frontend
1. Start the frontend: `npm run dev` in `frontend`
2. Navigate to Settings (user menu) → Feedback
3. Test customer feedback creation (as a customer)
4. Test admin view (as a manager/admin)

## 📝 Notes

- Pagination on admin list caps pageSize at 100 for performance
- All timestamps use UTC timezone through IDateTimeService
- Feedback title and content are validated for length constraints
- Customer name is included in FeedbackDto for admin visibility
- Cascade delete ensures feedbacks are removed when customer is deleted
- All responses use ApiResponse<T> wrapper for consistency

## 🚀 Future Enhancements

- Email notifications to admins for new feedback
- Feedback categories/tags
- Admin responses to feedback
- Feedback rating/importance levels
- Archive functionality
- Advanced filtering and search

---

**Status**: ✅ Complete and Ready for Testing
**Last Updated**: [Current Date]
