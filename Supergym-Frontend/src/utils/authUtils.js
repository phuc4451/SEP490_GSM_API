const getRole = () => {
  return localStorage.getItem("role");
};

// Check if user has required role
const hasRequiredRole = (requiredRoles) => {
  const userRole = getRole();

  // If requiredRoles is an array, check if user role is in the array
  if (Array.isArray(requiredRoles)) {
    return requiredRoles.includes(userRole);
  }

  // If requiredRoles is a single role
  return userRole === requiredRoles;
};

// Protected route wrapper
const checkAuth = (requiredRoles) => {
  const isAuthenticated = localStorage.getItem("token"); // Assuming you store JWT token
  const hasRole = hasRequiredRole(requiredRoles);

  if (!isAuthenticated) {
    window.location.href = "/login";
    return false;
  }

  if (!hasRole) {
    window.location.href = "/unauthorized"; // Or redirect to login
    return false;
  }

  return true;
};

const Logout = () => {
  localStorage.removeItem("token");
  localStorage.removeItem("role");
};

// Export functions
export { getRole, hasRequiredRole, checkAuth, Logout };
