// Preloader.jsx
import React from "react";
import "./Preloader.css"; // Nếu bạn có CSS cho Preloader

const Preloader = () => {
  return (
    <div id="js-preloader" className="js-preloader">
      <div className="preloader-inner">
        <span className="dot"></span>
        <div className="dots">
          <span></span>
          <span></span>
          <span></span>
        </div>
      </div>
    </div>
  );
};

export default Preloader;
