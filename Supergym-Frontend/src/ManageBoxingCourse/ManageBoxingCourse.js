import React, { useState, useEffect, useRef } from "react";

import axios from "axios";
import Header from "../Header/Header";
import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageBoxingCourse.css";
import courseImg from "../assets/images/features-first-icon.png";
import LocalOfferIcon from "@mui/icons-material/LocalOffer";
import CloseIcon from "@mui/icons-material/Close";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
const ManageBoxingCourse = () => {
  const fileInputRef = useRef(null);

  const [successMessage, setSuccessMessage] = useState("");
  const [customerToDelete, setCustomerToDelete] = useState(null); // for deletion
  const [currentCustomer, setCurrentCustomer] = useState(null); // for editing customer
  const [previewImage, setPreviewImage] = useState(null); // To store preview image
  const [errors, setErrors] = useState({});
  const [customerData, setCustomerData] = useState([]);
  const [errorMessage, setErrorMessage] = useState("");
  const errorModalRef = useRef(null);
  const [courses, setCourses] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [validationErrors, setValidationErrors] = useState({});
  const [formData, setFormData] = useState({
    boxingOptionId: "string",
    description: "",
    sessions: "",
    months: "",
    memberCount: "",
    totalPrice: "",
  });

  const openAddCustomerModal = () => {
    setFormData({
      boxingOptionId: "string",
      description: "",
      sessions: "",
      months: "",
      memberCount: "",
      totalPrice: "", // Đặt là chuỗi rỗng
    });
    setPreviewImage(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = null;
    }
    setCurrentCustomer(null);
    customerModalRef.current.style.display = "block";
    customerModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const openEditCustomerModal = (course) => {
    setFormData({
      boxingOptionId: "string",
      description: course.description,
      sessions: course.sessions,
      months: course.months,
      memberCount: course.memberCount,
      totalPrice: course.totalPrice,
    });
    setCurrentCustomer(course); // Lưu thông tin gói tập để biết được ID của gói cần chỉnh sửa
    customerModalRef.current.style.display = "block"; // Hiển thị modal
    customerModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const openDetailrModal = (course) => {
    setFormData({
      boxingOptionId: "string",
      description: course.description,
      sessions: course.sessions,
      months: course.months,
      memberCount: course.memberCount,
      totalPrice: course.totalPrice.toLocaleString(),
    });
    setCurrentCustomer(course); // Lưu thông tin gói tập để biết được ID của gói cần chỉnh sửa
    detailModalRef.current.style.display = "block"; // Hiển thị modal
    detailModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const closeModal = () => {
    detailModalRef.current.style.display = "none";
    detailModalRef.current.classList.remove("active");
    customerModalRef.current.style.display = "none";
    customerModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentCustomer(null); // Đặt lại khách hàng hiện tại
    setPreviewImage(null); // Đặt lại ảnh xem trước
    setFormData({
      boxingOptionId: "string",
      description: "string",
      sessions: 0,
      months: 0,
      memberCount: 0,
      totalPrice: 0,
    });
  };

  const deleteCustomer = async () => {
    if (customerToDelete) {
      const token = localStorage.getItem("token");
      try {
        await axios.delete(`http://localhost:5000/api/users/${customerToDelete.userId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        setCustomerData(customerData.filter((cus) => cus.id !== customerToDelete.id));
        closeModal();
        closeDeleteModal();
        showSuccessModal("Xóa người dùng thành công!");
      } catch (error) {
        console.error("Error deleting customer:", error);
      }
    }
  };
  const handleDeleteEmployee = (e) => {
    e.preventDefault();
    deleteCustomer();
  };

  const customerModalRef = useRef(null);
  const detailModalRef = useRef(null);
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);

  useEffect(() => {
    const fetchCourses = async () => {
      try {
        const token = localStorage.getItem("token");
        const response = await axios.get("http://localhost:5000/api/BoxingOption", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setCourses(response.data);
      } catch (error) {
        console.error("Error fetching course data:", error);
      }
    };

    fetchCourses();
  }, []);
  const formatNumberWithCommas = (number) => {
    return number.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
  };

  // Helper function to remove commas from string
  const removeCommas = (str) => {
    return str.replace(/,/g, "");
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;

    // For non-numeric fields (description)
    if (name === "description") {
      setFormData((prev) => ({
        ...prev,
        [name]: value,
      }));

      // Validate description
      if (!value || value.trim() === "") {
        setValidationErrors((prev) => ({
          ...prev,
          [name]: "Tên gói Boxing không được để trống",
        }));
      } else {
        setValidationErrors((prev) => {
          const newErrors = { ...prev };
          delete newErrors[name];
          return newErrors;
        });
      }
      return;
    }

    // Special handling for price fields
    if (name === "totalPrice") {
      // Remove existing commas first
      const cleanValue = removeCommas(value);

      // Allow empty value or backspace
      if (cleanValue === "") {
        setFormData((prev) => ({
          ...prev,
          [name]: "",
        }));
        setValidationErrors((prev) => ({
          ...prev,
          [name]: "Giá trị phải lớn hơn 0",
        }));
        return;
      }

      // Only process if the input is a valid number
      if (/^\d*$/.test(cleanValue)) {
        const numberValue = Number(cleanValue);

        // Format with commas for display
        const formattedValue = numberValue.toLocaleString("en-US").replace(/,/g, ",");

        setFormData((prev) => ({
          ...prev,
          [name]: formattedValue,
        }));

        if (numberValue <= 0) {
          setValidationErrors((prev) => ({
            ...prev,
            [name]: "Giá trị phải lớn hơn 0",
          }));
        } else {
          setValidationErrors((prev) => {
            const newErrors = { ...prev };
            delete newErrors[name];
            return newErrors;
          });
        }
      }
      return;
    }

    // For other numeric fields (sessions, memberCount)
    const numericFields = ["sessions", "memberCount"];

    if (numericFields.includes(name)) {
      // Allow empty value
      if (value === "") {
        setFormData((prev) => ({
          ...prev,
          [name]: "",
        }));
        setValidationErrors((prev) => ({
          ...prev,
          [name]: "Giá trị phải lớn hơn 0",
        }));
        return;
      }

      const numberValue = Number(value);
      if (!isNaN(numberValue)) {
        setFormData((prev) => ({
          ...prev,
          [name]: value,
        }));

        if (numberValue <= 0) {
          setValidationErrors((prev) => ({
            ...prev,
            [name]: "Giá trị phải lớn hơn 0",
          }));
        } else {
          setValidationErrors((prev) => {
            const newErrors = { ...prev };
            delete newErrors[name];
            return newErrors;
          });
        }
      }
    }
  };

  const validateField = (name, value) => {
    const newErrors = { ...errors };

    switch (name) {
      case "description":
        if (!value.trim()) {
          newErrors.description = "Vui lòng nhập tên gói Boxing";
        } else {
          delete newErrors.description;
        }
        break;
      case "totalPrice":
        if (!value || parseFloat(removeCommas(value)) <= 0) {
          newErrors.totalPrice = "Giá phải lớn hơn 0";
        } else {
          delete newErrors.totalPrice;
        }
        break;
      case "sessions":
        if (!value || parseInt(value) <= 0) {
          newErrors.sessions = "Số buổi phải lớn hơn 0";
        } else {
          delete newErrors.sessions;
        }
        break;
      case "months":
        if (!value || parseInt(value) <= 0) {
          newErrors.months = "Thời hạn phải lớn hơn 0";
        } else {
          delete newErrors.months;
        }
        break;
      case "memberCount":
        if (!value || parseInt(value) <= 0) {
          newErrors.memberCount = "Số thành viên phải lớn hơn 0";
        } else {
          delete newErrors.memberCount;
        }
        break;
      default:
        break;
    }

    setErrors(newErrors);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // Validate all fields before submission
    const errors = {};
    if (!formData.description || formData.description.trim() === "") {
      errors.description = "Tên gói Boxing không được để trống";
    }
    if (!formData.sessions || Number(formData.sessions) <= 0) {
      errors.sessions = "Số buổi phải lớn hơn 0";
    }
    if (!formData.memberCount || Number(formData.memberCount) <= 0) {
      errors.memberCount = "Số thành viên phải lớn hơn 0";
    }
    if (!formData.totalPrice || Number(removeCommas(formData.totalPrice)) <= 0) {
      errors.totalPrice = "Giá trị phải lớn hơn 0";
    }

    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }

    try {
      const token = localStorage.getItem("token");
      const formattedData = {
        boxingOptionId: "string",
        description: formData.description,
        sessions: parseInt(formData.sessions),
        months: 0,
        memberCount: parseInt(formData.memberCount),
        totalPrice: parseFloat(removeCommas(formData.totalPrice)),
      };

      const response = await axios.post("http://localhost:5000/api/BoxingOption", formattedData, {
        headers: { Authorization: `Bearer ${token}` },
      });

      setCourses((prevCourses) => [...prevCourses, response.data]);
      closeModal();
      showSuccessModal(`Gói Boxing ${formData.description} được lưu thành công`);
    } catch (error) {
      if (error.response && error.response.status === 409) {
        showErrorModal(`Gói Boxing ${formData.description} đã tồn tại, vui lòng thêm gói Boxing khác!`);
      } else {
        showErrorModal(`Lỗi khi tạo gói Boxing`);
      }
    }
  };

  const showErrorModal = (errMessage) => {
    setErrorMessage(errMessage);
    errorModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const closeErrorModal = () => {
    errorModalRef.current.style.display = "none"; // Ẩn modal
    // document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    // window.location.reload();
  };

  const showSuccessModal = (message) => {
    setSuccessMessage(message); // Cập nhật thông báo
    successModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  // Hàm để đóng modal thông báo
  const closeSuccessModal = () => {
    successModalRef.current.style.display = "none"; // Ẩn modal
    document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    window.location.reload();
  };
  const openDeleteModal = (customer) => {
    setCustomerToDelete(customer); // Set customer to be deleted
    deleteModalRef.current.style.display = "block"; // Show delete modal
    document.querySelector(".modal-overlay").style.display = "block"; // Show overlay
  };

  const closeDeleteModal = () => {
    deleteModalRef.current.style.display = "none"; // Hide delete modal
    document.querySelector(".modal-overlay").style.display = "none"; // Hide overlay
  };

  return (
    <>
      <Header />

      <section className="section" id="features">
        <div className="container">
          <div className="row">
            <div className="col-lg-6 offset-lg-3">
              <div className="section-heading">
                <h2>Danh sách các gói tập</h2>
                <button onClick={openAddCustomerModal} className="btn btn-success">
                  <AddCircleOutlineIcon />
                  <span>Thêm gói Boxing</span>
                </button>
              </div>
            </div>
          </div>

          <div className="row">
            {courses.map((course) => (
              <div className="col-lg-6" key={course.gymMembershipId}>
                <ul className="features-items">
                  <li className="feature-item">
                    <div className="left-icon">
                      <img src={courseImg} alt="Course" />
                    </div>
                    <div className="right-content">
                      <div className="course-action">
                        <h4>{course.description}</h4>
                        <div className="row row-course">
                          <div className="col-6">
                            <p>Số buổi: {course.sessions}</p>
                            {/* <p>Thời hạn: {course.months}</p> */}
                          </div>
                          <div className="col-6">
                            <p>Lượng thành viên: {course.memberCount}</p>
                          </div>
                        </div>
                        <a href="#" onClick={() => openDetailrModal(course)} className="btn-fix detail-button">
                          Chi tiết
                        </a>
                        {/* <a href="#" className="btn-fix edit-button">
                          Sửa
                        </a>
                        <a href="#" className="btn-fix delete-button">
                          Xóa
                        </a> */}
                      </div>

                      <div className="sale-price-section">
                        {/* <button id="add-sale-button" className="btn btn-primary sale-btn">
                          Áp dụng giảm giá
                          <LocalOfferIcon />
                        </button> */}
                        <p className="price">Giá: {course.totalPrice.toLocaleString()} VND</p>
                      </div>
                    </div>
                  </li>
                </ul>
              </div>
            ))}
          </div>
        </div>
      </section>

      {showForm && (
        <div className="modal">
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
              <form onSubmit={handleSubmit}>
                <div className="modal-header">
                  <h4 className="modal-title text-center mx-auto">{currentCustomer ? "Sửa gói tập" : "Thêm gói tập"}</h4>
                  <button type="button" className="close" onClick={() => setShowForm(false)}>
                    &times;
                  </button>
                </div>
                <div className="modal-body">
                  <div className="form-group">
                    <label>Tên gói tập</label>
                    <input type="text" className="form-control" name="name" value={formData.name} onChange={handleInputChange} required />
                  </div>
                  <div className="form-group">
                    <label>Thời gian (tháng)</label>
                    <input type="number" className="form-control" name="durationMonths" value={formData.durationMonths} onChange={handleInputChange} required />
                  </div>
                  <div className="form-group">
                    <label>Số buổi</label>
                    <input type="number" className="form-control" name="sessionCount" value={formData.sessionCount} onChange={handleInputChange} required />
                  </div>
                  <div className="form-group">
                    <label>Giá</label>
                    <input type="number" className="form-control" name="price" value={formData.price} onChange={handleInputChange} required />
                  </div>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-default" onClick={() => setShowForm(false)} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
                    Hủy
                  </button>
                  <button type="submit" className="btn btn-success">
                    Lưu
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
      {showForm && <div className="modal-overlay" onClick={() => setShowForm(false)}></div>}

      {/* customer Modal */}
      <div ref={customerModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="employeeForm" onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">{currentCustomer ? "Chi tiết gói Boxing" : "Thêm gói Boxing"}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Tên gói Boxing <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${validationErrors.description ? "is-invalid" : ""}`} name="description" value={formData.description} onChange={handleInputChange} />
                    {validationErrors.description && <div className="invalid-feedback">{validationErrors.description}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Thành viên trong gói <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className={`form-control ${validationErrors.memberCount ? "is-invalid" : ""}`} name="memberCount" value={formData.memberCount} onChange={handleInputChange} />
                    {validationErrors.memberCount && <div className="invalid-feedback">{validationErrors.memberCount}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Giá <span className="icon-input">(*)</span>
                    </label>
                    <div className={`input-group ${validationErrors.totalPrice ? "is-invalid" : ""}`}>
                      <input type="text" className={`form-control ${validationErrors.totalPrice ? "is-invalid" : ""}`} name="totalPrice" value={formData.totalPrice} onChange={handleInputChange} />
                      <span className={`input-readonly ${validationErrors.totalPrice ? "error-border" : ""}`}>VNĐ</span>
                    </div>
                    {validationErrors.totalPrice && <div className="invalid-feedback d-block">{validationErrors.totalPrice}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Số buổi <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className={`form-control ${validationErrors.sessions ? "is-invalid" : ""}`} name="sessions" value={formData.sessions} onChange={handleInputChange} />
                    {validationErrors.sessions && <div className="invalid-feedback">{validationErrors.sessions}</div>}
                  </div>
                </div>
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeModal} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
                  Hủy
                </button>

                <button type="submit" className="btn btn-success">
                  Lưu
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      <div ref={detailModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="employeeForm" onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">{currentCustomer ? "Chi tiết gói Boxing" : "Thêm gói Boxing"}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* First row of input fields */}
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Tên gói Boxing <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className="form-control" name="description" value={formData.description} onChange={handleInputChange} required disabled />
                    {errors.description && <div className="error-message">{errors.description}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Thành viên trong gói <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className="form-control" name="memberCount" value={formData.memberCount} onChange={handleInputChange} required disabled />
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Giá <span className="icon-input">(*)</span>
                    </label>

                    <div className="input-group">
                      <input type="text" className="form-control" name="totalPrice" value={formData.totalPrice} onChange={handleInputChange} required disabled />
                      <span className="input-readonly">VNĐ</span>
                    </div>
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Số buổi <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className="form-control" name="sessions" value={formData.sessions} onChange={handleInputChange} required disabled />
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>
                </div>
              </div>

              {/* <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeModal} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
                  Hủy
                </button>

                <button type="submit" className="btn btn-success">
                  Lưu
                </button>
              </div> */}
            </form>
          </div>
        </div>
      </div>
      {/* Modal Overlay */}
      <div className="modal-overlay"></div>

      {/* Delete Modal */}
      <div ref={deleteModalRef} className="modal">
        <div className="modal-dialog modal-dialog-notify">
          <div className="modal-content">
            <form id="deleteEmployeeForm" onSubmit={handleDeleteEmployee}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">Xóa người dùng</h4>
                <a type="button" className="close" onClick={closeDeleteModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                <p>Bạn có chắc chắn muốn xóa {customerToDelete?.name}?</p>
                <p className="text-warning">
                  <small>Hành động này sẽ không được hoàn tác.</small>
                </p>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeModal} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
                  Hủy
                </button>

                <button type="submit" className="btn btn-success">
                  Lưu
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      {/* Success Modal */}
      <div ref={successModalRef} className="modal">
        <div className="modal-dialog modal-dialog-notify">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Thông báo</h4>
              <a type="button" className="close" onClick={closeSuccessModal}>
                <CloseIcon />
              </a>
            </div>
            <div className="modal-body">
              <p>{successMessage}</p>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-primary" onClick={closeSuccessModal}>
                OK
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Error Modal */}
      <div ref={errorModalRef} className="modal">
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
    </>
  );
};

export default ManageBoxingCourse;
