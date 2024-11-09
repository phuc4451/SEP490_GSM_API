// Signin.js
import React, { useState } from "react";
import Preloader from "../Preloader/Preloader";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import logo from "../assets/images/super-gym-logo.jpg";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowRightFromBracket } from "@fortawesome/free-solid-svg-icons";

import "./Signin.css";

function Signin() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);
  const [isLoading, setIsLoading] = useState(false);

  // const handleLogin = async (event) => {
  //   event.preventDefault();

  //   try {
  //     const response = await axios.post("http://localhost:5000/api/auth/login", {
  //       email,
  //       password,
  //     });
  //     if (response.status === 200) {
  //       const token = response.data.jwTtoken; // Giả sử token được trả về trong response
  //       localStorage.setItem("token", token); // Lưu token vào localStorage
  //       navigate("/dashboard");
  //     }
  //   } catch (err) {
  //     setError("Đăng nhập thất bại. Vui lòng kiểm tra lại tài khoản và mật khẩu.");
  //     console.error(err);
  //   }
  // };

  const handleLogin = async (event) => {
    event.preventDefault();
    setIsLoading(true);  // Show preloader during login attempt

    try {
      const response = await axios.post("http://localhost:5000/api/auth/login", { email, password });
      if (response.status === 200) {
        const token = response.data.jwTtoken;
        localStorage.setItem("token", token);
        navigate("/dashboard");
      }
    } catch (err) {
      setError("Đăng nhập thất bại. Vui lòng kiểm tra lại tài khoản và mật khẩu.");
      console.error(err);
    } finally {
      setIsLoading(false);  // Hide preloader after login attempt
    }
  };
  return (
    <div className="app-signin-container">
    {isLoading ? <Preloader /> : <div>{/* Nội dung khác của ManageCustomer */}</div>}

      <div className="login-container">
        <div className="login-box">
          <img src={logo} alt="logo" className="logo-signin" />
          <form onSubmit={handleLogin}>
            <div className="input-group-signin">
              <input className="input-signin" type="text" placeholder="Tài khoản" required value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>
            <div className="input-group">
              <input className="input-signin" type="password" placeholder="Mật khẩu" required value={password} onChange={(e) => setPassword(e.target.value)} />
            </div>
            {error && <p className="error-message">{error}</p>}
            <button type="submit" className="login-btn">
              Đăng nhập
            </button>
          </form>
          <p className="register-link">
            <a href="/signup">
              Đăng kí tài khoản
              <FontAwesomeIcon icon={faArrowRightFromBracket} />
            </a>
          </p>

          <p className="register-link">
            <a href="#">Quên mật khẩu</a>
          </p>
        </div>
      </div>
    </div>
  );
}

export default Signin;
