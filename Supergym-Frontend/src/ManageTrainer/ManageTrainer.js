import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "../assets/css/common.css";
import "./ManageTrainer.css";

import SearchIcon from "@mui/icons-material/Search";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import { Delete } from "@mui/icons-material";

const ManageTrainer = () => {
  const [trainerDataList, setTrainerDataList] = useState([]);
  //PRELOAD
  const [trainerData, setTrainerData] = useState([]);
  const [isDataLoading, setIsDataLoading] = useState(true);
  const [isTimeoutFinished, setIsTimeoutFinished] = useState(false);
  //END PRELOAD

  const [currentTrainer, setCurrentTrainer] = useState(null); // for editing trainer
  const [trainerToDelete, setTrainerToDelete] = useState(null); // for deletion
  const [trainerToAddCourse, setTrainerToAddCourse] = useState(null); // for deletion
  const [previewImage, setPreviewImage] = useState(null); // To store preview image
  const fileInputRef = useRef(null);
  const [successMessage, setSuccessMessage] = useState("");
  const [rentalOptions, setRentalOptions] = useState([]);
  const [boxingOptions, setBoxingOptions] = useState([]);
  const [selectedOption, setSelectedOption] = useState(""); // to store selected option

  const [formData, setFormData] = useState({
    trainerId: "string",
    userId: "string",
    name: "string",
    isTrainerGym: true,
    isTrainerBoxing: true,
    bio: "string",
    specialization: "string",
  });
  const [errors, setErrors] = useState({});
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 4; // Maximum 2 trainers per page

  const trainerModalRef = useRef(null);
  const addTrainToCourseModalRef = useRef(null);
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);

  //FETCH DATA AND PRELOAD
  useEffect(() => {
    const fetchTrainers = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Trainer/GetAllTrainersWithOptions", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setTrainerData(response.data);
      } catch (error) {
        console.error("Error fetching trainers:", error);
      } finally {
        setIsDataLoading(false);
      }
    };
    fetchTrainers();
  }, []);

  useEffect(() => {
    const fetchOptions = async () => {
      const token = localStorage.getItem("token"); // Retrieve the token from localStorage
      try {
        // Fetch Rental Options
        const rentalResponse = await axios.get("http://localhost:5000/api/RentalOption", {
          headers: { Authorization: `Bearer ${token}` }, // Include token in the headers
        });

        // Fetch Boxing Options
        const boxingResponse = await axios.get("http://localhost:5000/api/BoxingOption", {
          headers: { Authorization: `Bearer ${token}` }, // Include token in the headers
        });

        // Gộp cả rentalOptions và boxingOptions thành 1 mảng với field type
        const combinedOptions = [...rentalResponse.data.map((option) => ({ ...option, type: "rental" })), ...boxingResponse.data.map((option) => ({ ...option, type: "boxing" }))];

        setRentalOptions(combinedOptions); // Set the combined options state
      } catch (error) {
        console.error("Error fetching options:", error); // Log any errors that occur
      }
    };

    fetchOptions(); // Call fetchOptions when the component mounts
  }, []); // Empty dependency array ensures this effect only runs once on mount

  // Log the options to verify
  useEffect(() => {
    console.log(rentalOptions, boxingOptions);
  }, [rentalOptions, boxingOptions]);

  useEffect(() => {
    const timer = setTimeout(() => {
      setIsTimeoutFinished(true); // Timeout finished
    }, 500);

    return () => clearTimeout(timer); // Clear timeout on unmount
  }, []);

  const isLoading = isDataLoading || !isTimeoutFinished;
  //END FETCH DATA AND PRELOAD

  //PAGENATION
  const totalPages = Math.ceil(trainerData.length / itemsPerPage);
  const currentTrainers = trainerData.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);
  const formatDate = (dob) => {
    const date = dob?.date ?? "--";
    const month = dob?.month ?? "--";
    const year = dob?.year ?? "----";
    return `${date}/${month}/${year}`;
  };
  const handlePageChange = (pageNumber) => {
    setCurrentPage(pageNumber);
  };
  //END PAGENATION

  const openAddTrainerModal = () => {
    setFormData({
      trainerId: "string",
      userId: "string",
      name: "string",
      isTrainerGym: true,
      isTrainerBoxing: true,
      bio: "string",
      specialization: "string",
    });
    setPreviewImage(null); // Đặt lại ảnh xem trước
    if (fileInputRef.current) {
      fileInputRef.current.value = null; // Đặt lại giá trị input file
    }
    setCurrentTrainer(null); // Đặt lại trainer hiện tại để thêm mới
    trainerModalRef.current.style.display = "block"; // Hiển thị modal
    trainerModalRef.current.classList.add("active"); // Thêm class 'active'
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const openEditTrainerModal = (trainer) => {
    const dob = trainer.dob || {};
    const formattedDob = `${dob.year || "----"}-${String(dob.month || "01").padStart(2, "0")}-${String(dob.date || "01").padStart(2, "0")}`;

    setFormData({
      trainerId: trainer.trainerId,
      userId: trainer.userId,
      name: trainer.name,
      isTrainerGym: trainer.isTrainerGym,
      isTrainerBoxing: trainer.isTrainerBoxing,
      bio: trainer.bio,
      specialization: trainer.specialization,
    });
    setCurrentTrainer(trainer);
    trainerModalRef.current.style.display = "block";
    trainerModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const closeModal = () => {
    addTrainToCourseModalRef.current.style.display = "none";
    addTrainToCourseModalRef.current.classList.remove("active");
    trainerModalRef.current.style.display = "none";
    trainerModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentTrainer(null); // Đặt lại trainer hiện tại
    setPreviewImage(null); // Đặt lại ảnh xem trước
    setFormData({
      trainerId: "string",
      userId: "string",
      name: "string",
      isTrainerGym: true,
      isTrainerBoxing: true,
      bio: "string",
      specialization: "string",
    });
  };

  const handleInputChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    });
  };

  const validateForm = () => {
    const newErrors = {};
    if (!formData.name || formData.name.length < 5 || formData.name.length > 30) {
      newErrors.name = "Họ tên phải từ 5 đến 30 ký tự.";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0; // Return true if no errors
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
  const openDeleteModal = (trainer) => {
    setTrainerToDelete(trainer); // Set trainer to be deleted
    deleteModalRef.current.style.display = "block"; // Show delete modal
    document.querySelector(".modal-overlay").style.display = "block"; // Show overlay
  };

  const openAddTrainToCourseModal = (trainer) => {
    setFormData({
      trainerId: trainer.trainerId,
      userId: trainer.userId,
      name: trainer.name,
      isTrainerGym: trainer.isTrainerGym,
      isTrainerBoxing: trainer.isTrainerBoxing,
      bio: trainer.bio,
      specialization: trainer.specialization,
    });
    setTrainerToAddCourse(trainer); // Set trainer to be deleted
    addTrainToCourseModalRef.current.style.display = "block"; // Show delete modal
    document.querySelector(".modal-overlay").style.display = "block"; // Show overlay
  };

  const closeDeleteModal = () => {
    deleteModalRef.current.style.display = "none"; // Hide delete modal
    document.querySelector(".modal-overlay").style.display = "none"; // Hide overlay
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (validateForm()) {
      const token = localStorage.getItem("token");

      // Format date to the required format
      const dob = new Date(formData.dob);
      const dobData = {
        date: dob.getDate(),
        month: dob.getMonth() + 1,
        year: dob.getFullYear(),
      };

      // Create a basic trainer data object
      const trainerData = {
        trainerId: currentTrainer ? currentTrainer.trainerId : "string",
        userId: formData.userId,
        name: formData.name,
        isTrainerGym: formData.isTrainerGym,
        isTrainerBoxing: formData.isTrainerBoxing,
        bio: formData.bio,
        specialization: formData.specialization,
      };

      // Include gymMembershipId only for adding a new trainer
      if (!currentTrainer) {
        trainerData.gymMembershipId = "string"; // Include gymMembershipId for new trainers
        trainerData.password = formData.password; // Include password for new trainers
      }

      try {
        if (currentTrainer) {
          // Update trainer
          await axios.patch(`http://localhost:5000/api/Trainer/updateTrainer/${currentTrainer.trainerId}`, trainerData, {
            headers: { Authorization: `Bearer ${token}` },
          });

          // Update trainer data in the list
          setTrainerDataList((prevData) => prevData.map((trainer) => (trainer.userId === currentTrainer.userId ? { ...trainer, ...trainerData } : trainer)));
        } else {
          // Add new trainer
          const response = await axios.post("http://localhost:5000/api/Trainer/addTrainer", trainerData, {
            headers: { Authorization: `Bearer ${token}` },
          });

          setTrainerDataList([...trainerDataList, response.data]);
        }

        // Reset form and close modal
        setFormData({
          trainerId: "",
          userId: "",
          name: "",
          isTrainerGym: true,
          isTrainerBoxing: true,
          bio: "",
          specialization: "",
        });
        closeModal();
        showSuccessModal("Huấn luyện viên được lưu thành công");
      } catch (error) {
        console.error("Error saving trainer:", error);
      }
    }
  };

  const deleteTrainer = async () => {
    if (trainerToDelete) {
      const token = localStorage.getItem("token");
      try {
        await axios.delete(`http://localhost:5000/api/users/${trainerToDelete.userId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        setTrainerData(trainerData.filter((trainer) => trainer.id !== trainerToDelete.id));
        closeModal();
        closeDeleteModal();
        showSuccessModal("Xóa huấn luyện viên thành công!");
      } catch (error) {
        console.error("Error deleting trainer:", error);
      }
    }
  };
  const handleDeleteTrainer = (e) => {
    e.preventDefault();
    deleteTrainer();
  };

  const handleSubmitAddTrainerToCourse = async (e) => {
    e.preventDefault(); // Ngăn hành vi mặc định của form

    if (trainerToAddCourse && selectedOption) {
      const token = localStorage.getItem("token");

      // Chuẩn bị dữ liệu gửi tới API
      const data = {
        trainerRentalPlanId: "string", // Bạn cần gán ID hợp lệ tại đây nếu cần
        trainerId: trainerToAddCourse.trainerId, // ID của huấn luyện viên đã chọn
        rentalOptionId: selectedOption, // rentalOptionId là giá trị ID của gói đã chọn
      };

      try {
        // Gửi yêu cầu API để thêm huấn luyện viên vào khóa học
        const response = await axios.post(
          "http://localhost:5000/api/trainerRentalPlan",
          data, // Gửi payload
          {
            headers: { Authorization: `Bearer ${token}` }, // Thêm token vào header
          }
        );

        // // Cập nhật dữ liệu khi thành công
        // setTrainerData((prevData) =>
        //   prevData.map((trainer) =>
        //     trainer.trainerId === trainerToAddCourse.trainerId
        //       ? { ...trainer, isAssignedToCourse: true } // Đánh dấu huấn luyện viên đã được thêm vào khóa học
        //       : trainer
        //   )
        // );

        // Hiển thị thông báo thành công và đóng modal
        closeModal(); // Đóng modal
        showSuccessModal("Huấn luyện viên đã được thêm vào khóa học thành công!");
      } catch (error) {
        console.error("Error adding trainer to course:", error);
        showSuccessModal("Có lỗi khi thêm huấn luyện viên vào khóa học.");
      }
    } else {
      // Nếu không chọn huấn luyện viên hoặc gói, hiển thị thông báo lỗi
      showSuccessModal("Vui lòng chọn huấn luyện viên và gói!");
    }
  };

  const handleImageChange = (e) => {
    const file = e.target.files[0];
    if (file) {
      setFormData({
        ...formData,
        userAvatar: file,
      });

      const reader = new FileReader();
      reader.onloadend = () => {
        setPreviewImage(reader.result);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleCheckboxChange = (e) => {
    const { name, checked } = e.target;
    setFormData({
      ...formData,
      [name]: checked, // set to true if checked, false otherwise
    });
  };

  return (
    <>
      <Header />

      {isLoading ? <Preloader /> : <div>{/* Nội dung khác của ManageTrainer */}</div>}
      {/* <!-- ***** Preloader End ***** --> */}

      <div className="user-select">
        <h1>Quản lý huấn luyện viên trong hệ thống super gym</h1>

        <div className="select-search-container">
          <div className="search-container">
            <input type="text" id="searchUser" className="form-control" placeholder="Tìm kiếm..." />
            <span className="search-icon">
              <SearchIcon />
            </span>
          </div>

          <select className="form-control  form-select" id="selectRole">
            <option value="">Chọn vai trò</option>
            <option value="admin">Admin</option>
            <option value="staff">Nhân viên</option>
          </select>

          <select className="form-control form-select" id="selectStatus">
            <option value="">Chọn trạng thái</option>
            <option value="active">Hoạt động</option>
            <option value="inactive">Không hoạt động</option>
          </select>

          <select className="form-control form-select" id="selectGender">
            <option value="">Chọn giới tính</option>
            <option value="male">Nam</option>
            <option value="female">Nữ</option>
          </select>
        </div>
      </div>

      <div className="table-container">
        <div className="table-wrapper">
          <div className="table-title">
            <div className="row">
              <div className="col-sm-6">
                <h2>Quản lí huấn luyện viên</h2>
              </div>
              <div className="col-sm-6">
                <button onClick={openAddTrainerModal} className="btn btn-success">
                  <AddCircleOutlineIcon />
                  <span>Thêm mới huấn luyện viên</span>
                </button>
              </div>
            </div>
          </div>

          <table className="table table-hover table-fixed">
            <thead>
              <tr>
                <th className="name-el">Họ tên</th>
                <th>Loại huấn luyện viên</th>
                <th>Chuyên môn</th>
                {/* <th>bio</th> */}

                <th className="action-el">Hành động</th>
              </tr>
            </thead>
            <tbody>
              {currentTrainers.map((trainer, index) => (
                <tr key={index}>
                  <td>
                    {/* Display trainer avatar if available, otherwise fallback */}
                    <img src={trainer.userAvatar ? `data:image/jpeg;base64,${trainer.userAvatar}` : "/path/to/default-avatar.jpg"} alt="Avatar" className="trainer-avatar" />
                    {trainer.name}
                  </td>
                  <td>
                    {/* Display multiple types if both Gym and Boxing are true */}
                    {trainer.isTrainerGym && trainer.isTrainerBoxing ? "Gym, Boxing" : trainer.isTrainerGym ? "Gym" : trainer.isTrainerBoxing ? "Boxing" : "N/A"}
                  </td>

                  <td>{trainer.specialization || "Chưa có chuyên môn"}</td>
                  {/* <td>{trainer.bio || 'Chưa có thông tin'}</td> */}
                  <td>
                    {/* Add the actions: Edit and Delete */}
                    <a href="#" onClick={() => openEditTrainerModal(trainer)} className="edit">
                      <EditIcon />
                    </a>
                    <a href="#" onClick={() => openDeleteModal(trainer)} className="delete">
                      <DeleteIcon />
                    </a>
                    <a href="#" onClick={() => openAddTrainToCourseModal(trainer)} className="add">
                      <AddCircleOutlineIcon />
                    </a>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Pagination */}
          <div className="clearfix-el">
            <div className="hint-text">
              Showing <b>{currentTrainers.length}</b> out of <b>{trainerData.length}</b> entries
            </div>
            <ul className="pagination">
              <li className={`page-item ${currentPage === 1 ? "disabled" : ""}`}>
                <a href="#" onClick={() => handlePageChange(currentPage - 1)} className="page-link">
                  <ChevronLeftIcon />
                </a>
              </li>
              {Array.from({ length: totalPages }, (_, index) => (
                <li key={index} className={`page-item ${currentPage === index + 1 ? "active" : ""}`}>
                  <a href="#" onClick={() => handlePageChange(index + 1)} className="page-link">
                    {index + 1}
                  </a>
                </li>
              ))}
              <li className={`page-item ${currentPage === totalPages ? "disabled" : ""}`}>
                <a href="#" onClick={() => handlePageChange(currentPage + 1)} className="page-link">
                  <ChevronRightIcon />
                </a>
              </li>
            </ul>
          </div>
        </div>
      </div>

      {/* Trainer Modal */}
      <div ref={trainerModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="trainerForm" onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">{currentTrainer ? "Sửa thông tin huấn luyện viên" : "Thêm huấn luyện viên"}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* First row of input fields */}
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Họ tên <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className="form-control" name="name" value={formData.name || ""} onChange={handleInputChange} required />
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Chuyên môn</label>
                    <input type="text" className="form-control" name="specialization" value={formData.specialization || ""} onChange={handleInputChange} required />
                    {errors.specialization && <div className="error-message">{errors.specialization}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Tiểu sử</label>
                    <input type="text" className="form-control" name="bio" value={formData.bio || ""} onChange={handleInputChange} required />
                    {errors.bio && <div className="error-message">{errors.bio}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Ảnh nhận diện <span className="icon-input">(*)</span>
                    </label>
                    <input type="file" className="form-control" onChange={handleImageChange} accept="image/*" ref={fileInputRef} />
                  </div>
                  {previewImage && (
                    <div className="form-group col">
                      <img src={previewImage} alt="Ảnh xem trước" className="preview-image" />
                    </div>
                  )}
                </div>

                {/* Gym and Boxing checkboxes */}
                <div className="row">
                  <div className="form-group col">
                    <label>Loại huấn luyện viên</label>
                    <div className="checkbox-group">
                      <div>
                        <input
                          type="checkbox"
                          id="isTrainerGym"
                          name="isTrainerGym"
                          checked={formData.isTrainerGym} // checked is true or false
                          onChange={handleCheckboxChange}
                        />
                        <label htmlFor="isTrainerGym">Gym</label>

                        <input
                          type="checkbox"
                          id="isTrainerBoxing"
                          name="isTrainerBoxing"
                          checked={formData.isTrainerBoxing} // checked is true or false
                          onChange={handleCheckboxChange}
                        />
                        <label htmlFor="isTrainerBoxing">Boxing</label>
                      </div>
                    </div>
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

      {/* Add Trainer to Course Modal */}
      <div ref={addTrainToCourseModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="addTrainerToCourseForm" onSubmit={handleSubmitAddTrainerToCourse}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">
                  Thêm trainer <strong>{formData.name || "(hlv)"}</strong> vào gói
                </h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* First row of input fields */}
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Họ tên <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className="form-control" name="name" value={formData.name || ""} onChange={handleInputChange} required />
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>

                  <div className="form-group col">
                    <label>Chọn gói</label>
                    <select
                      className="form-control"
                      name="option"
                      value={selectedOption}
                      onChange={(e) => setSelectedOption(e.target.value)} // Cập nhật selected option
                      required
                    >
                      <option value="">-- Chọn gói --</option>
                      {rentalOptions.map((option, index) => (
                        <option key={index} value={option.rentalOptionId}>
                          {option.description} {/* Hiển thị description của gói */}
                        </option>
                      ))}
                    </select>
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

      {/* Modal Overlay */}
      <div className="modal-overlay"></div>

      {/* Delete Modal */}
      <div ref={deleteModalRef} className="modal">
        <div className="modal-dialog modal-dialog-notify">
          <div className="modal-content">
            <form id="deleteTrainerForm" onSubmit={handleDeleteTrainer}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">Xóa huấn luyện viên</h4>
                <a type="button" className="close" onClick={closeDeleteModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                <p>Bạn có chắc chắn muốn xóa {trainerToDelete?.name}?</p>
                <p className="text-warning">
                  <small>Hành động này sẽ không được hoàn tác.</small>
                </p>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeDeleteModal}>
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
    </>
  );
};

export default ManageTrainer;
