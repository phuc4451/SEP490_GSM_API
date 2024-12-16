import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import "./Header.css";
import AccountCircleIcon from "@mui/icons-material/AccountCircle";
import PowerSettingsNewIcon from "@mui/icons-material/PowerSettingsNew";
import logo from "../assets/images/super-gym-logo.jpg";

const Header = () => {
  const navigate = useNavigate();
  const [customerData, setCustomerData] = useState([]);
  const role = localStorage.getItem("role");

  useEffect(() => {
    const fetchUserData = async () => {
      try {
        const token = localStorage.getItem("token");
        if (!token) {
          console.error("Token not found");
          return;
        }

        const response = await axios.get("http://localhost:5000/api/Users/GetCurrentUser", {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        setCustomerData([{
          userId: response.data.userId,
          name: response.data.name,
          email: response.data.email,
          userAvatar: `data:image/png;base64,${response.data.userAvatar}`,
        }]);
      } catch (error) {
        console.error("Error fetching user data:", error);
      }
    };

    fetchUserData();
  }, []);

  const handleLogout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("role");
    navigate("/login");
  };

  const isAdmin = role === "admin";

  const shouldShowMenu = (menuType) => {
    if (isAdmin) return true;
    if (role === "staff") {
      return ![
        "staff-management", 
        "dashboard", 
        "feedback", 
        "trainer-management",
        "manageShift",  // Added to hide shift management
        "manageSalary"  // Added to hide salary management
      ].includes(menuType);
    }
    return true;
  };

  return (
    <nav className="navbar navbar-expand-xl navbar-light bg-light">
      <button type="button" className="navbar-toggler" data-toggle="collapse" data-target="#navbarCollapse">
        <span className="navbar-toggler-icon"></span>
      </button>
      <div id="navbarCollapse" className="collapse navbar-collapse justify-content-start">
        <div className="navbar-nav">
          <img src={logo} alt="logo" className="logo-manage" />
        </div>

        <div className="navbar-nav">
          <a href="/" className="nav-item nav-link active">
            Trang chủ
          </a>

          {shouldShowMenu("dashboard") && (
            <a href="/dashboard" className="nav-item nav-link active">
              Thống kê
            </a>
          )}

          <div className="nav-item dropdown">
            <a href="#" className="nav-link active dropdown-toggle">
              Quản lý người dùng
            </a>
            <div className="dropdown-content">
              {shouldShowMenu("staff-management") && <a href="/manageManager">Quản lý nhân sự</a>}
              <a href="/manageCustomer">Quản lý khách hàng</a>
              {shouldShowMenu("trainer-management") && (
                <div className="dropdown-submenu">
                  <a href="#" className="dropright-toggle">
                    Quản lý huấn luyện viên
                  </a>
                  <div className="submenu-content">
                    <a href="/ManageTrainer">Thêm huấn luyện viên</a>
                    <a href="/EditTrainer">Sửa thông tin huấn luyện viên</a>
                  </div>
                </div>
              )}
              <a href="/manageMembership">Quản lý Membership</a>
            </div>
          </div>

          <a href="/manageEquipment" className="nav-item nav-link active">
            Quản lý thiết bị
          </a>

          <div className="nav-item dropdown">
            <a href="#" className="nav-link active dropdown-toggle">
              Quản lý gói
            </a>
            <div className="dropdown-content dropdown-content-package">
              <a href="/manageCourse">Quản lý gói tập Gym</a>
              <a href="/manageTrainerCourse">Quản lý gói Trainer</a>
              <a href="/manageBoxingCourse">Quản lý gói Boxing</a>
            </div>
          </div>

          {shouldShowMenu("feedback") && (
            <a href="/manageFeedback" className="nav-item nav-link active">
              Quản lý phản hồi
            </a>
          )}

          <a href="/manageSchedule" className="nav-item nav-link active">
            Quản lý lịch
          </a>

          {shouldShowMenu("manageShift") && (
            <a href="/manageShift" className="nav-item nav-link active">
              Quản lý ca làm 
            </a>
          )}

          {shouldShowMenu("manageSalary") && (
            <a href="/manageSalary" className="nav-item nav-link active">
              Quản lý lương
            </a>
          )}
        </div>

        <div className="navbar-nav ms-auto">
          <div className="nav-item dropdown">
            <a href="#" className="nav-link dropdown-toggle user-action">
              <img src={customerData[0]?.userAvatar || logo} alt="user-avatar" className="user-avatar" />
              <span>{customerData[0]?.name || "Guest"}</span>
              <b className="caret"></b>
            </a>

            <div className="dropdown-content">
              <a href="/profile">
                <AccountCircleIcon /> Thông tin cá nhân
              </a>
              <div className="dropdown-divider"></div>
              <a href="#" onClick={handleLogout}>
                <PowerSettingsNewIcon /> Đăng xuất
              </a>
            </div>
          </div>
        </div>
      </div>
    </nav>
  );
};

export default Header;