import React, { useState, useRef } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

const DeleteModal = ({ employee, onDelete, onClose }) => {
  return (
    <div className="modal" style={{ display: "block" }} id="deleteEmployeeModal">
      <div className="modal-dialog modal-dialog-notify">
        <div className="modal-content">
          <form
            id="deleteEmployeeForm"
            onSubmit={(e) => {
              e.preventDefault();
              onDelete(employee);
            }}
          >
            <div className="modal-header">
              <h4 className="modal-title">Xóa người dùng</h4>
              <button type="button" className="close" onClick={onClose}>
                &times;
              </button>
            </div>
            <div className="modal-body">
              <p>
                Bạn có chắc muốn xóa người dùng <strong>{employee?.name}</strong>?
              </p>
              <p className="text-warning">
                <small>Hành động này sẽ không thể hoàn tác.</small>
              </p>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-default" onClick={onClose}>
                Hủy
              </button>
              <button type="submit" className="btn btn-danger">
                Xóa
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default DeleteModal;
