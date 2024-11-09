import React from "react";
import CloseIcon from "@mui/icons-material/Close";

const errorModal = ({ showErrorModal, closeErrorModal, errorMessage }) => {
  if (!showErrorModal) return null; // Nếu không cần hiển thị thì return null

  return (
    <div className="modal">
      <div className="modal-dialog modal-dialog-notify">
        <div className="modal-content">
          <div className="modal-header">
            <h4 className="modal-title text-center mx-auto">Thông báo</h4>
            <a type="button" className="close" onClick={closeErrorModal}>
              <CloseIcon />
            </a>
          </div>
          <div className="modal-body">
            <p>{errorMessage}</p>
          </div>
          <div className="modal-footer">
            <button type="button" className="btn btn-primary" onClick={closeErrorModal}>
              OK
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default errorModal;
