import React, { useEffect, useState } from "react";
import axios from "axios";
import { useNavigate } from "react-router-dom";
import { Helmet } from "react-helmet";
import "bootstrap/dist/css/bootstrap.min.css";
import "./Header.css";

import NotificationsIcon from "@mui/icons-material/Notifications";
import MailIcon from "@mui/icons-material/Mail";
import AccountCircleIcon from "@mui/icons-material/AccountCircle";
import SettingsIcon from "@mui/icons-material/Settings";
import PowerSettingsNewIcon from "@mui/icons-material/PowerSettingsNew";

import logo from "../assets/images/super-gym-logo.jpg";

const Header = () => {
  const navigate = useNavigate();
  
  const [customerData, setCustomerData] = useState([]);

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

        setCustomerData([
          {
            name: response.data.name,
            userAvatar: `data:image/png;base64,${response.data.userAvatar}`,
          },
        ]);
      } catch (error) {
        console.error("Error fetching user data:", error);
      }
    };

    fetchUserData();
  }, []);

  const handleLogout = () => {
    localStorage.removeItem("token");
    navigate("/login");
  };

  return (
    <>
      <Helmet>
        <title>Header</title>
      </Helmet>
      <nav className="navbar navbar-expand-xl navbar-light bg-light">
        <button type="button" className="navbar-toggler" data-toggle="collapse" data-target="#navbarCollapse">
          <span className="navbar-toggler-icon"></span>
        </button>
        <div id="navbarCollapse" className="collapse navbar-collapse justify-content-start">
          <div className="navbar-nav">
            <img src={logo} alt="logo" className="logo-manage" />
          </div>

          <div className="navbar-nav">
            <a href="/" className="nav-item nav-link active">Trang chủ</a>
            <a href="/dashboard" className="nav-item nav-link active">Thống kê</a>

            {/* User Management Dropdown */}
            <div className="nav-item dropdown">
              <a href="" className="nav-link active dropdown-toggle" id="userManagementDropdown">
                Quản lý người dùng
              </a>
              <div className="dropdown-content">
                <a href="/managManager">Quản lý nhân sự</a>
                <a href="/manageCustomer" >Quản lý khách hàng</a>
                <a href="/manageCustomer" >Quản lý PT</a>
              </div>
            </div>

            <a href="/manageEquipment" className="nav-item nav-link active">Quản lý thiết bị</a>
            <div className="nav-item dropdown">
              <a href="" className="nav-link active dropdown-toggle" id="courseManagementDropdown">
                Quản lý gói
              </a>
              <div className="dropdown-content">
                <a href="/manageCourse">Quản lý gói tập</a>
                <a href="/manageSale" >Quản lý gói giảm giá</a>
              </div>
            </div>
            <a href="/manageFeedback" className="nav-item nav-link active">Quản lý phản hồi</a>
            <a href="/manageSchedule" className="nav-item nav-link active">Quản lý lịch</a>
          </div>

          <div className="navbar-nav ms-auto">
            <a href="#" className="nav-item nav-link header-icon notifications">
              <NotificationsIcon />
              <span className="badge">1</span>
            </a>
            <a href="#" className="nav-item nav-link header-icon messages">
              <MailIcon />
              <span className="badge">10</span>
            </a>

            {/* User Dropdown */}
            <div className="nav-item dropdown">
              <a href="#" className="nav-link dropdown-toggle user-action" id="userDropdown">
                <img
                  src={customerData[0]?.userAvatar || logo}
                  alt="user-avatar"
                  className="user-avatar"
                />
                <span>{customerData[0]?.name || "Guest"}</span>
                <b className="caret"></b>
              </a>

              <div className="dropdown-content">
                <a href="#">
                  <AccountCircleIcon /> Thông tin cá nhân
                </a>
                <a href="#">
                  <SettingsIcon /> Đổi mật khẩu
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
    </>
  );
};

export default Header;
