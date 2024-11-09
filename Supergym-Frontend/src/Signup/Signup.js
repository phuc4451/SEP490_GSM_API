import React, { useState } from "react";
import Preloader from "../Preloader/Preloader";
import axios from "axios";
import { Helmet } from "react-helmet";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowRightFromBracket } from "@fortawesome/free-solid-svg-icons";

import logo from "../assets/images/super-gym-logo.jpg";
import "./Signup.css";

function Signup() {
  const [formData, setFormData] = useState({
    username: "",
    password: "",
    confirmPassword: "",
    email: "",
  });
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [successMessage, setSuccessMessage] = useState("");

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.id]: e.target.value,
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);

    if (formData.password !== formData.confirmPassword) {
      setErrorMessage("Mật khẩu và Xác nhận mật khẩu không khớp!");
      setSuccessMessage(""); // Clear success message on error
      setIsLoading(false);
      return;
    }

    setErrorMessage("");
    setError(""); // Clear previous error message

    try {
      const response = await axios.post("http://localhost:5000/api/Auth/register", {
        email: formData.email,
        password: formData.password,
        name: formData.username,
      });
      if (response.status === 200) {
        setSuccessMessage("Vui lòng vào email đăng ký để xác thực tài khoản.");
        setError(""); // Clear error message on success
      }
    } catch (err) {
      setError("Email đã được đăng kí");
      setSuccessMessage(""); // Clear success message on error
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="app-signup-container">
      {isLoading ? <Preloader /> : null}

      <div className="login-container">
        <div className="login-box">
          <img src={logo} alt="logo" className="logo-signup" />
          <form id="registerForm" onSubmit={handleSubmit}>
            <div className="input-group-signup">
              <input className="input-signup" type="email" id="email" placeholder="Email" required onChange={handleChange} />
            </div>
            <div className="input-group-signup">
              <input className="input-signup" type="password" id="password" placeholder="Mật khẩu" required onChange={handleChange} />
            </div>
            <div className="input-group-signup">
              <input className="input-signup" type="password" id="confirmPassword" placeholder="Xác nhận mật khẩu" required onChange={handleChange} />
            </div>
            <div className="input-group-signup">
              <input className="input-signup" type="text" id="username" placeholder="Tên tài khoản" required onChange={handleChange} />
            </div>
            <button type="submit" className="login-btn">
              Đăng kí
            </button>
            {errorMessage && <p id="error-message">{errorMessage}</p>}
            {error && <p id="error-message">{error}</p>}
            {successMessage && <p id="success-message">{successMessage}</p>}
          </form>
          <p className="register-link">
            <a href="/login">
              Quay trở lại đăng nhập
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

export default Signup;
