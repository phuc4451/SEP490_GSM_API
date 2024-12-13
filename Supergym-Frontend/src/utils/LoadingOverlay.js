// LoadingOverlay.jsx
import React from "react";

const LoadingOverlay = ({ isLoading }) => {
  if (!isLoading) return null;

  return (
    <div
      className="modal-overlay"
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        width: "100%",
        height: "100%",
        backgroundColor: "rgba(0, 0, 0, 0.5)",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        zIndex: 9999,
      }}
    >
      <div className="spinner-container">
        <div
          className="spinner-border text-danger"
          role="status"
          style={{
            width: "5rem", // Tăng kích thước từ 3rem lên 5rem
            height: "5rem", // Tăng kích thước từ 3rem lên 5rem
            borderWidth: "0.35em", // Làm đường viền dày hơn
            color: "#dc0000", // Màu đỏ đậm hơn
            opacity: "0.9", // Làm đậm hơn một chút
          }}
        >
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    </div>
  );
};

export default LoadingOverlay;