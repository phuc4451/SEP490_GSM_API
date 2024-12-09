// Signin.js
import React, { useState } from "react";
import Preloader from "../Preloader/Preloader";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import logo from "../assets/images/super-gym-logo.jpg";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowRightFromBracket } from "@fortawesome/free-solid-svg-icons";

// import "./Signin.css";

function ForgotPassword() {
    const navigate = useNavigate();
    const [email, setEmail] = useState("");
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(null); // Thêm state để lưu thông báo thành công
    const [isLoading, setIsLoading] = useState(false);
  
    const handleForgotpassword = async (event) => {
      event.preventDefault();
      setIsLoading(true);
      // Reset messages
      setError(null);
      setSuccess(null);
      
      try {
        const response = await axios.post("http://localhost:5000/api/auth/forgot-password", { email });
        
        // Kiểm tra response và set thông báo thành công
        if (response.data && response.data.message) {
          setSuccess(response.data.message);
        } else {
          setSuccess("Yêu cầu đã được gửi. Vui lòng kiểm tra email của bạn.");
        }
        
        // Clear input sau khi gửi thành công
        setEmail("");
        
      } catch (err) {
        console.error(err);
        if (err.response && err.response.status === 403) {
          setError(err.response.data.message);
        } else if (err.response && err.response.data.message) {
          setError(err.response.data.message);
        } else {
          setError("Đã xảy ra lỗi. Vui lòng thử lại sau.");
        }
      } finally {
        setIsLoading(false);
      }
    };
  
    return (
      <div className="app-signin-container">
        {isLoading ? <Preloader /> : null}
  
        <div className="login-container">
          <div className="login-box">
            <img src={logo} alt="logo" className="logo-signin" />
            <form onSubmit={handleForgotpassword}>
              <div className="input-group-signin">
                <input 
                  className="input-signin" 
                  type="email" 
                  placeholder="Nhập email đã đăng kí" 
                  required 
                  value={email} 
                  onChange={(e) => setEmail(e.target.value)} 
                />
              </div>
              {error && <p className="error-message">{error}</p>}
              {success && <p className="success-message">{success}</p>}
              <button type="submit" className="login-btn">
                Gửi yêu cầu
              </button>
            </form>
  
            <p className="register-link">
              <a href="/login">Quay trở lại đăng nhập</a>
            </p>
          </div>
        </div>
      </div>
    );
  }

export default ForgotPassword;
