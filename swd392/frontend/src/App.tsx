import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { GoogleOAuthProvider } from '@react-oauth/google';
import { AuthProvider } from './contexts/AuthContext';
import { useAuth } from './hooks/useAuth';
import Layout from './components/layout/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import GlobalErrorNotification from './components/GlobalErrorNotification';
import Home from './pages/Home';
import Login from './pages/Login';
import Register from './pages/Register';
import TodaysMenu from './pages/TodaysMenu';
import WeeklyMenu from './pages/WeeklyMenu';
import MyCart from './pages/MyCart';
import MyOrders from './pages/MyOrders';
import StaffOrderList from './pages/StaffOrderList';
import DeliverySchedule from './pages/DeliverySchedule';
import MyDeliverySchedule from './pages/MyDeliverySchedule';
import MealPlans from './pages/MealPlans';
import CreateMealPlan from './pages/CreateMealPlan';
import CreateAIMealPlan from './pages/CreateAIMealPlan';
import MealPlanDetail from './pages/MealPlanDetail';
import EditMealPlan from './pages/EditMealPlan';
import ActiveMeals from './pages/ActiveMeals';
import Recipes from './pages/Recipes';
import RecipeDetail from './pages/RecipeDetail';
import Ingredients from './pages/Ingredients';
import VirtualFridge from './pages/VirtualFridge';
import GroceryList from './pages/GroceryList';
import NutrientCalculator from './pages/NutrientCalculator';
import Orders from './pages/Orders';
import StaffOrders from './pages/StaffOrders';
import CreateOrder from './pages/CreateOrder';
import OrderDetail from './pages/OrderDetail';
import PaymentCallback from './pages/PaymentCallback';
import Profile from './pages/Profile';
import DeliveryTracking from './pages/DeliveryTracking';
import MyDeliveries from './pages/MyDeliveries';
import DeliveryDetail from './pages/DeliveryDetail';
import AdminIngredients from './pages/AdminIngredients';
import AdminAllergies from './pages/AdminAllergies';
import AdminRecipes from './pages/AdminRecipes';
import AdminNutrients from './pages/AdminNutrients';
import AdminAICreditPackages from './pages/AdminAICreditPackages';
import AdminSubscriptionPackages from './pages/AdminSubscriptionPackages';
import AdminMenu from './pages/AdminMenu';
import AdminAccounts from './pages/AdminAccounts';
import SystemConfiguration from './pages/SystemConfiguration';
import RevenueReport from './pages/RevenueReport';
import AdminDashboard from './pages/AdminDashboard';
import AICredits from './pages/AICredits';
import AICreditCallback from './pages/AICreditCallback';
import Feedback from './pages/Feedback';

function AppRoutes() {
  const { isAuthenticated } = useAuth();

  return (
    <Routes>
      <Route path="/login" element={
        isAuthenticated ? <Navigate to="/" replace /> : <Login />
      } />
      <Route path="/register" element={
        isAuthenticated ? <Navigate to="/" replace /> : <Register />
      } />
      
      <Route element={<Layout />}>
        <Route path="/" element={<Home />} />
        <Route path="/todays-menu" element={
          <ProtectedRoute>
            <TodaysMenu />
          </ProtectedRoute>
        } />
        <Route path="/weekly-menu" element={
          <ProtectedRoute>
            <WeeklyMenu />
          </ProtectedRoute>
        } />
        <Route path="/cart" element={
          <ProtectedRoute>
            <MyCart />
          </ProtectedRoute>
        } />
        <Route path="/my-orders" element={
          <ProtectedRoute>
            <MyOrders />
          </ProtectedRoute>
        } />
        <Route path="/staff/order-list" element={
          <ProtectedRoute>
            <StaffOrderList />
          </ProtectedRoute>
        } />
        <Route path="/staff/delivery-schedule" element={
          <ProtectedRoute>
            <DeliverySchedule />
          </ProtectedRoute>
        } />
        <Route path="/deliveryman/my-schedule" element={
          <ProtectedRoute>
            <MyDeliverySchedule />
          </ProtectedRoute>
        } />
        <Route path="/admin/menu" element={
          <ProtectedRoute>
            <AdminMenu />
          </ProtectedRoute>
        } />
        <Route path="/admin/accounts" element={
          <ProtectedRoute>
            <AdminAccounts />
          </ProtectedRoute>
        } />
        <Route path="/admin/ingredients" element={
          <ProtectedRoute>
            <AdminIngredients />
          </ProtectedRoute>
        } />
        <Route path="/admin/allergies" element={
          <ProtectedRoute>
            <AdminAllergies />
          </ProtectedRoute>
        } />
        <Route path="/admin/recipes" element={
          <ProtectedRoute>
            <AdminRecipes />
          </ProtectedRoute>
        } />
        <Route path="/admin/nutrients" element={
          <ProtectedRoute>
            <AdminNutrients />
          </ProtectedRoute>
        } />
        <Route path="/admin/ai-credit-packages" element={
          <ProtectedRoute>
            <AdminAICreditPackages />
          </ProtectedRoute>
        } />
        <Route path="/admin/subscription-packages" element={
          <ProtectedRoute>
            <AdminSubscriptionPackages />
          </ProtectedRoute>
        } />
        <Route path="/admin/system-config" element={
          <ProtectedRoute>
            <SystemConfiguration />
          </ProtectedRoute>
        } />
        <Route path="/admin/dashboard" element={
          <ProtectedRoute requiredRole="Admin">
            <AdminDashboard />
          </ProtectedRoute>
        } />
        <Route path="/admin/revenue-report" element={
          <ProtectedRoute>
            <RevenueReport />
          </ProtectedRoute>
        } />
        <Route path="/meal-plans" element={
          <ProtectedRoute>
            <MealPlans />
          </ProtectedRoute>
        } />
        <Route path="/meal-plans/create" element={
          <ProtectedRoute>
            <CreateMealPlan />
          </ProtectedRoute>
        } />
        <Route path="/meal-plans/create-ai" element={
          <ProtectedRoute>
            <CreateAIMealPlan />
          </ProtectedRoute>
        } />
        <Route path="/meal-plans/:id" element={
          <ProtectedRoute>
            <MealPlanDetail />
          </ProtectedRoute>
        } />
        <Route path="/meal-plans/:id/edit" element={
          <ProtectedRoute>
            <EditMealPlan />
          </ProtectedRoute>
        } />
        <Route path="/active-meals" element={
          <ProtectedRoute>
            <ActiveMeals />
          </ProtectedRoute>
        } />
        <Route path="/recipes" element={
          <ProtectedRoute>
            <Recipes />
          </ProtectedRoute>
        } />
        <Route path="/recipes/:id" element={
          <ProtectedRoute>
            <RecipeDetail />
          </ProtectedRoute>
        } />
        <Route path="/ingredients" element={
          <ProtectedRoute>
            <Ingredients />
          </ProtectedRoute>
        } />
        <Route path="/fridge" element={
          <ProtectedRoute>
            <VirtualFridge />
          </ProtectedRoute>
        } />
        <Route path="/grocery-list" element={
          <ProtectedRoute>
            <GroceryList />
          </ProtectedRoute>
        } />
        <Route path="/nutrient-calculator" element={
          <ProtectedRoute>
            <NutrientCalculator />
          </ProtectedRoute>
        } />
        <Route path="/orders" element={
          <ProtectedRoute>
            <Orders />
          </ProtectedRoute>
        } />
        <Route path="/staff/orders" element={
          <ProtectedRoute>
            <StaffOrders />
          </ProtectedRoute>
        } />
        <Route path="/orders/create" element={
          <ProtectedRoute>
            <CreateOrder />
          </ProtectedRoute>
        } />
        <Route path="/orders/:id" element={
          <ProtectedRoute>
            <OrderDetail />
          </ProtectedRoute>
        } />
        <Route path="/payment/callback" element={
          <ProtectedRoute>
            <PaymentCallback />
          </ProtectedRoute>
        } />
        <Route path="/track-delivery/:orderId" element={
          <ProtectedRoute>
            <DeliveryTracking />
          </ProtectedRoute>
        } />
        <Route path="/deliveries" element={
          <ProtectedRoute>
            <MyDeliveries />
          </ProtectedRoute>
        } />
        <Route path="/deliveries/:deliveryId" element={
          <ProtectedRoute>
            <DeliveryDetail />
          </ProtectedRoute>
        } />
        <Route path="/profile" element={
          <ProtectedRoute>
            <Profile />
          </ProtectedRoute>
        } />
        <Route path="/feedback" element={
          <ProtectedRoute>
            <Feedback />
          </ProtectedRoute>
        } />
        <Route path="/ai-credits" element={
          <ProtectedRoute>
            <AICredits />
          </ProtectedRoute>
        } />
        <Route path="/ai-credits/callback" element={
          <AICreditCallback />
        } />
      </Route>
    </Routes>
  );
}

function App() {
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;

  if (!clientId) {
    return (
      <Router>
        <div style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
          <h1>Google OAuth is not configured</h1>
          <p>Set VITE_GOOGLE_CLIENT_ID in your frontend .env file to enable Google sign in.</p>
        </div>
      </Router>
    );
  }
  
  return (
    <GoogleOAuthProvider clientId={clientId}>
      <Router>
        <AuthProvider>
          <GlobalErrorNotification />
          <AppRoutes />
        </AuthProvider>
      </Router>
    </GoogleOAuthProvider>
  );
}

export default App;
