import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "../assets/css/common.css";
import "./ManageEquipment.css";
import * as XLSX from "xlsx";

import SearchIcon from "@mui/icons-material/Search";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import FileDownloadIcon from "@mui/icons-material/FileDownload";
import LoadingSpinner from "../utils/LoadingOverlay";

import ErrorModal from "../assets/hook/modal/errorModal.js";
const ManageEquipment = () => {
  const [equipmentList, setEquipmentList] = useState([]);
  const [isDataLoading, setIsDataLoading] = useState(true);
  const [modalTitle, setModalTitle] = useState("");
  const [showImportQuantityField, setShowImportQuantityField] = useState(false);
  const [isEditingCode, setIsEditingCode] = useState(false); // Trạng thái để hiển thị nút Xác nhận
  const [isAddingNew, setIsAddingNew] = useState(false);
  const [isExistingEquipment, setIsExistingEquipment] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [selectedEquipmentId, setSelectedEquipmentId] = useState(null);

  const itemsPerPage = 13;

  const equipmentModalRef = useRef(null);
  const deleteModalRef = useRef(null);
  const successModalRef = useRef(null);
  const [file, setFile] = useState(null);
  const [isSubmitDisabled, setIsSubmitDisabled] = useState(false);

  const errorModalRef = useRef(null);

  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [errors, setErrors] = useState({});
  const [currentEquipment, setCurrentEquipment] = useState(null); // for editing equipment
  const [searchCode, setSearchCode] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [importHistoryData, setImportHistoryData] = useState([]);
  const [showImportHistoryModal, setShowImportHistoryModal] = useState(false);
  const importHistoryModalRef = useRef(null);
  const [formData, setFormData] = useState({
    equipmentName: "",
    equipmentCode: "",
    equipmentImportPrice: "",
    equipmentBrand: "",
    equipmentQuantity: "",
    equipmentManufactured: "",
    equipmentCategoryId: "",
    equipmentCategoryName: "",
    trainingRoomId: "",
    trainingRoomName: "",
    equipmentSize: "",
    equipmentWeightStack: "",
    equipmentMaterial: "",
    importQuantity: "",
  });

  const defaultEquipmentData = {
    equipmentId: "",
    equipmentName: "",
    equipmentCode: "",
    equipmentImportPrice: 0,
    equipmentBrand: "",
    equipmentQuantity: 0,
    equipmentCategoryId: 0,
    trainingRoomId: 0,
    equipmentManufactured: "",
    equipmentSize: "",
    equipmentWeightStack: 0,
    equipmentMaterial: "",
  };

  const fetchImportHistory = async () => {
    const token = localStorage.getItem("token");
    try {
      const response = await axios.get("http://localhost:5000/api/ImportEquipment", {
        headers: { Authorization: `Bearer ${token}` },
      });
      setImportHistoryData(response.data);
      importHistoryModalRef.current.style.display = "block";
      document.querySelector(".modal-overlay").style.display = "block";
    } catch (error) {
      console.error("Error fetching import history:", error);
      showErrorModal("Không thể lấy dữ liệu lịch sử nhập thiết bị");
    }
  };

  const closeImportHistoryModal = () => {
    importHistoryModalRef.current.style.display = "none";
    document.querySelector(".modal-overlay").style.display = "none";
  };

  const formatNumberWithCommas = (value) => {
    return value.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;

    if (name === "equipmentImportPrice") {
      // Cho phép trường input trống
      if (!value) {
        setFormData({
          ...formData,
          equipmentImportPrice: "",
        });
        setErrors((prevErrors) => ({
          ...prevErrors,
          equipmentImportPrice: "Giá nhập là bắt buộc",
        }));
        return;
      }

      // Xóa dấu phẩy từ giá trị nhập vào để kiểm tra
      const numericValue = value.replace(/,/g, "");

      // Kiểm tra nếu là số và lớn hơn 0
      if (!isNaN(numericValue) && parseFloat(numericValue) > 0) {
        setFormData({
          ...formData,
          equipmentImportPrice: formatNumberWithCommas(numericValue),
        });
        setErrors((prevErrors) => ({
          ...prevErrors,
          equipmentImportPrice: "",
        }));
      } else {
        setFormData({
          ...formData,
          equipmentImportPrice: value,
        });
        setErrors((prevErrors) => ({
          ...prevErrors,
          equipmentImportPrice: "Giá nhập phải là số dương",
        }));
      }
    } else {
      setFormData({
        ...formData,
        [name]: value,
      });
      validateField(name, value);
    }
  };

  const validateField = (name, value) => {
    let errorMsg = "";

    // Kiểm tra các trường hợp cụ thể cho từng trường
    switch (name) {
      case "equipmentName":
        if (!value) errorMsg = "Tên thiết bị là bắt buộc.";
        else if (value.length < 6) errorMsg = "Tên thiết bị phải có ít nhất 6 ký tự.";
        break;
      case "equipmentCode":
        if (!value) errorMsg = "Mã thiết bị là bắt buộc.";
        else if (value.length < 3) errorMsg = "Mã thiết bị phải có ít nhất 3 ký tự.";
        break;
      case "equipmentImportPrice":
        if (!value) {
          errorMsg = "Giá nhập là bắt buộc";
        } else {
          const numericValue = value.replace(/,/g, "");
          if (isNaN(numericValue) || parseFloat(numericValue) <= 0) {
            errorMsg = "Giá nhập phải là số dương";
          }
        }
        break;
      case "equipmentBrand":
        if (!value) errorMsg = "Nhãn hiệu là bắt buộc.";
        else if (value.length < 3) errorMsg = "Nhãn hiệu phải có ít nhất 3 ký tự.";
        break;
      case "equipmentQuantity":
        if (!value) {
          errorMsg = "Số lượng là bắt buộc.";
        } else if (isNaN(value) || value < 0) {
          errorMsg = "Số lượng phải là số không âm.";
        } else if (currentEquipment && parseInt(value) > currentEquipment.equipmentQuantity) {
          errorMsg = "Số lượng không thể vượt quá số lượng hiện tại.";
        }
        break;
      case "equipmentManufactured":
        if (!value) errorMsg = "Nơi sản xuất là bắt buộc.";
        else if (value.length < 2) errorMsg = "Nhãn hiệu phải có ít nhất 2 ký tự.";

        break;
      case "equipmentSize":
        if (!value) errorMsg = "Kích thước là bắt buộc.";
        break;
      case "equipmentWeightStack":
        if (!value) errorMsg = "Trọng lượng tải là bắt buộc.";
        else if (isNaN(value) || value < 0) errorMsg = "Trọng lượng tải phải là số không âm.";
        break;
      case "equipmentMaterial":
        if (!value) errorMsg = "Chất liệu là bắt buộc.";
        else if (value.length < 3) errorMsg = "Chất liệu phải có ít nhất 3 ký tự.";

        break;
      default:
        break;
    }

    setErrors((prevErrors) => ({
      ...prevErrors,
      [name]: errorMsg,
    }));
  };

  const isFormValid = () => {
    const requiredFields = ["equipmentName", "equipmentCode", "equipmentImportPrice", "equipmentBrand", "equipmentQuantity", "equipmentManufactured", "equipmentSize", "equipmentWeightStack", "equipmentMaterial"];

    let isValid = true;
    requiredFields.forEach((field) => {
      if (!formData[field] || errors[field]) {
        validateField(field, formData[field]);
        isValid = false;
      }
    });

    return isValid;
  };

  const handleRadioChange = (event) => {
    const { name, value } = event.target;
    setFormData({
      ...formData,
      [name]: Number(value),
    });
  };

  const handleFileChange = (event) => {
    setFile(event.target.files[0]);
  };

  const handleFileUpload = async () => {
    if (!file) {
      alert("Vui lòng chọn một file trước khi tải lên.");
      return;
    }

    const formData = new FormData();
    formData.append("file", file);

    const token = localStorage.getItem("token");
    try {
      const response = await axios.post("http://localhost:5000/api/Equipment/importExcel", formData, {
        headers: {
          "Content-Type": "multipart/form-data",
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.status === 200) {
        showSuccessModal("Tải file Excel thành công!");
      } else {
        showErrorModal("Tải file thất bại. Vui lòng thử lại.");
      }
    } catch (error) {
      console.error("Error uploading file:", error);
      showErrorModal("Đã xảy ra lỗi khi tải file.");
    }
  };
  //=====================SEARCH EQUIPMENT=====================

  const handleSearchCodeChange = (e) => setSearchCode(e.target.value);

  const searchEquipmentByCode = async () => {
    const token = localStorage.getItem("token");
    try {
      const response = await axios.get(`http://localhost:5000/api/Equipment/searchByCode?equipmentCode=${searchCode}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.data) {
        // Bind the found data to formData and format equipmentImportPrice
        setFormData({
          equipmentName: response.data.equipmentName,
          equipmentCode: response.data.equipmentCode,
          equipmentImportPrice: formatNumberWithCommas(response.data.equipmentImportPrice), // Format with commas
          equipmentBrand: response.data.equipmentBrand,
          equipmentQuantity: response.data.equipmentQuantity,
          equipmentManufactured: response.data.equipmentManufactured,
          equipmentCategoryId: response.data.equipmentCategoryId,
          equipmentCategoryName: response.data.equipmentCategoryName,
          trainingRoomId: response.data.trainingRoomId,
          trainingRoomName: response.data.trainingRoomName,
          equipmentSize: response.data.equipmentSize,
          equipmentWeightStack: response.data.equipmentWeightStack,
          equipmentMaterial: response.data.equipmentMaterial,
        });
        setSelectedEquipmentId(response.data.equipmentId);
      } else {
        // Nếu không tìm thấy thiết bị, reset formData về giá trị mặc định
        setFormData(defaultEquipmentData);
        setCurrentEquipment(null);
        showErrorModal("Thiết bị không tìm thấy!");
      }
    } catch (error) {
      console.error("Error searching equipment:", error);
      setFormData(defaultEquipmentData);
      setCurrentEquipment(null);
      showErrorModal("Không tìm thấy mã thiết bị.");
    }
  };

  //=====================PAGE=====================
  const totalPages = Math.ceil(equipmentList.length / itemsPerPage);
  const currentEquipments = equipmentList.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);
  const handlePageChange = (pageNumber) => {
    setCurrentPage(pageNumber);
  };

  //=====================LISTUSER=====================
  useEffect(() => {
    const fetchEquipments = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Equipment", {
          headers: { Authorization: `Bearer ${token}` },
        });

        // Lọc các thiết bị có số lượng > 0
        const filteredEquipments = response.data.filter((equipment) => equipment.equipmentQuantity > 0);

        setEquipmentList(filteredEquipments);
      } catch (error) {
        console.error("Error fetching equipment data:", error);
      } finally {
        setIsDataLoading(false);
      }
    };
    fetchEquipments();
  }, []);

  // Các hàm mở modal khác nhau cho "Thiết bị có sẵn" và "Thiết bị mới"
  const openAddExistingEquipmentModal = () => {
    setFormData({
      equipmentName: "",
      equipmentCode: "",
      equipmentImportPrice: "",
      equipmentBrand: "",
      equipmentQuantity: "",
      equipmentManufactured: "",
      equipmentCategoryId: 1,
      equipmentCategoryName: "",
      trainingRoomId: 1,
      trainingRoomName: "",
      equipmentSize: "",
      equipmentWeightStack: "",
      equipmentMaterial: "",
    });
    setModalTitle("Nhập thiết bị có sẵn"); // Cập nhật tiêu đề
    setSearchCode("");
    setCurrentEquipment(null);
    setIsAddingNew(false); // Đặt chế độ không phải thêm mới
    setIsEditingCode(false);
    setIsExistingEquipment(true); // Đặt chế độ thiết bị có sẵn để disable các trường
    equipmentModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const openAddNewEquipmentModal = () => {
    setFormData({
      equipmentName: "",
      equipmentCode: "",
      equipmentImportPrice: "",
      equipmentBrand: "",
      equipmentQuantity: "",
      equipmentManufactured: "",
      equipmentCategoryId: 1,
      equipmentCategoryName: "",
      trainingRoomId: 1,
      trainingRoomName: "",
      equipmentSize: "",
      equipmentWeightStack: "",
      equipmentMaterial: "",
    });
    setModalTitle("Nhập thiết bị mới"); // Cập nhật tiêu đề
    setSearchCode("");
    setCurrentEquipment(null);
    setIsAddingNew(true); // Đặt chế độ thêm mới
    setIsEditingCode(false);
    setIsExistingEquipment(false);
    equipmentModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const openEditEquipmentModal = (equipment) => {
    setFormData({
      equipmentId: equipment.equipmentId,
      equipmentName: equipment.equipmentName,
      equipmentCode: equipment.equipmentCode,
      equipmentImportPrice: equipment.equipmentImportPrice.toLocaleString(),
      equipmentBrand: equipment.equipmentBrand,
      equipmentQuantity: equipment.equipmentQuantity,
      equipmentManufactured: equipment.equipmentManufactured,
      equipmentCategoryId: equipment.equipmentCategoryId,
      equipmentCategoryName: equipment.equipmentCategoryName,
      trainingRoomId: equipment.trainingRoomId,
      trainingRoomName: equipment.trainingRoomName,
      equipmentSize: equipment.equipmentSize,
      equipmentWeightStack: equipment.equipmentWeightStack,
      equipmentMaterial: equipment.equipmentMaterial,
    });
    setModalTitle("Sửa thông tin thiết bị");
    setCurrentEquipment(equipment);
    setIsEditingCode(false);
    setIsAddingNew(false);
    equipmentModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const handleSubmit = async (e, isConfirmation = false) => {
    e.preventDefault();
    if (!isFormValid()) {
      showErrorModal("Vui lòng điền đầy đủ và đúng các trường thông tin.");
      return;
    }
    const token = localStorage.getItem("token");

    // Chuẩn bị completeData và chuyển đổi equipmentImportPrice thành số
    const completeData = {
      ...defaultEquipmentData,
      ...formData,
      equipmentImportPrice: parseFloat(formData.equipmentImportPrice.replace(/,/g, "")), // Xóa dấu phẩy
    };

    const importEquipmentInput = parseInt(formData.importQuantity, 10);
    try {
      let response;
      if (currentEquipment) {
        // Update existing equipment
        await axios.put(`http://localhost:5000/api/Equipment/updateWithoutCodeCheck/${currentEquipment.equipmentId}`, completeData, {
          headers: { Authorization: `Bearer ${token}` },
        });
        setEquipmentList((prev) => prev.map((eq) => (eq.equipmentId === currentEquipment.equipmentId ? completeData : eq)));
      } else if (importEquipmentInput > 0) {
        // Update and then call ImportEquipment API
        await axios.put(`http://localhost:5000/api/Equipment/UpdateImport/${selectedEquipmentId}?importQuantity=${importEquipmentInput}`, completeData, {
          headers: { Authorization: `Bearer ${token}` },
        });

        setEquipmentList((prev) => prev.map((eq) => (eq.equipmentId === selectedEquipmentId ? completeData : eq)));

        // Prepare data for the ImportEquipment API
        const currentDate = new Date();
        const importDate = {
          date: currentDate.getDate(),
          month: currentDate.getMonth() + 1,
          year: currentDate.getFullYear(),
        };

        const importData = {
          importEquipmentId: "string",
          importDate,
          importQuantity: importEquipmentInput,
          importPrice: completeData.equipmentImportPrice,
          importTotalPrice: completeData.equipmentImportPrice * importEquipmentInput,
          equipmentId: selectedEquipmentId,
        };

        // Call the ImportEquipment API
        await axios.post("http://localhost:5000/api/ImportEquipment", importData, {
          headers: { Authorization: `Bearer ${token}` },
        });
      } else {
        // Add new equipment
        response = await axios.post("http://localhost:5000/api/Equipment/importNewEquipment", completeData, {
          headers: { Authorization: `Bearer ${token}` },
        });
        setEquipmentList([...equipmentList, response.data]);

        // Extract necessary data for the ImportEquipment API call
        const { equipmentId } = response.data;

        // Get current date in the required format
        const currentDate = new Date();
        const importDate = {
          date: currentDate.getDate(),
          month: currentDate.getMonth() + 1,
          year: currentDate.getFullYear(),
        };

        const importData = {
          importEquipmentId: "string",
          importDate,
          importQuantity: completeData.equipmentQuantity,
          importPrice: completeData.equipmentImportPrice,
          importTotalPrice: completeData.equipmentImportPrice * completeData.equipmentQuantity,
          equipmentId,
        };

        // Call the ImportEquipment API
        await axios.post("http://localhost:5000/api/ImportEquipment", importData, {
          headers: { Authorization: `Bearer ${token}` },
        });
      }

      closeModal();
      showSuccessModal("Thiết bị được lưu thành công!");
    } catch (error) {
      if (error.response && error.response.status === 409) {
        showErrorModal("Mã thiết bị đã tồn tại. Vui lòng nhập mã khác.");
      } else {
        console.error("Error saving equipment:", error);
      }
    }
  };

  //=====================SUBMIT DELETE=====================
  const deleteEquipment = async () => {
    if (currentEquipment) {
      const token = localStorage.getItem("token");
      try {
        await axios.delete(`http://localhost:5000/api/Equipment/${currentEquipment.equipmentId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        setEquipmentList(equipmentList.filter((eq) => eq.equipmentId !== currentEquipment.equipmentId));
        closeModal(); // Close equipment modal if it's open
        closeDeleteModal(); // Close delete modal if it's closed already
        showSuccessModal("Thiết bị đã bị xóa thành công!");
      } catch (error) {
        console.error("Error deleting equipment:", error);
      }
    }
  };

  const handleDeleteEquipment = (e) => {
    e.preventDefault();
    deleteEquipment();
  };

  //=====================MODAL=====================
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

  const closeSuccessModal = () => {
    successModalRef.current.style.display = "none"; // Ẩn modal
    document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    window.location.reload();
  };

  const openDeleteModal = (equipment) => {
    setCurrentEquipment(equipment);
    deleteModalRef.current.style.display = "block"; // Show delete modal
    document.querySelector(".modal-overlay").style.display = "block"; // Show overlay
  };

  const closeDeleteModal = () => {
    deleteModalRef.current.style.display = "none"; // Hide delete modal
    document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    // window.location.reload();
  };

  const closeModal = () => {
    equipmentModalRef.current.style.display = "none";
    equipmentModalRef.current.classList.remove("active");
    setIsExistingEquipment(false);
    setIsEditingCode(false);
    setIsSubmitDisabled(false);
    setErrors({}); // Clear errors
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentEquipment(null); // Reset current equipment to null when closing modal
  };
  //=====================EDIT EQUIPMENT CODE =====================
  // Hàm mở chế độ sửa mã thiết bị
  const handleEditCodeClick = () => {
    setIsEditingCode(true); // Kích hoạt chế độ sửa
    setIsSubmitDisabled(true); // Vô hiệu hóa nút "Lưu"
  };
  // Hàm xác nhận chỉnh sửa mã thiết bị
  const handleConfirmCodeClick = async (e) => {
    e.preventDefault();

    if (!isFormValid()) {
      showErrorModal("Vui lòng điền đầy đủ và đúng các trường thông tin.");
      return;
    }

    const token = localStorage.getItem("token");

    // Create the data object with the current equipment ID
    const updatedData = {
      ...formData,
      equipmentId: currentEquipment.equipmentId, // Use currentEquipment.equipmentId
      equipmentImportPrice: parseFloat(formData.equipmentImportPrice.replace(/,/g, "")), // Convert price to number
    };

    try {
      // Gọi API cập nhật thiết bị
      await axios.put(`http://localhost:5000/api/Equipment/${currentEquipment.equipmentId}`, updatedData, {
        headers: { Authorization: `Bearer ${token}` },
      });

      // Cập nhật danh sách thiết bị với dữ liệu đã chỉnh sửa
      setEquipmentList((prev) =>
        prev.map((eq) =>
          eq.equipmentId === currentEquipment.equipmentId
            ? {
                ...eq,
                ...updatedData,
              }
            : eq
        )
      );

      // Đặt lại chế độ chỉnh sửa và đóng modal
      setIsEditingCode(false);
      setIsSubmitDisabled(false);
      closeModal();
      showSuccessModal("Thiết bị được cập nhật thành công!");
    } catch (error) {
      if (error.response && error.response.status === 409) {
        showErrorModal("Mã thiết bị đã tồn tại. Vui lòng nhập mã khác.");
      } else {
        console.error("Error saving equipment:", error);
        showErrorModal("Có lỗi xảy ra khi cập nhật thiết bị.");
      }
    }
  };

  const handleCancelEditClick = () => {
    setIsEditingCode(false);
    setIsSubmitDisabled(false);
    // Reset dữ liệu hoặc trạng thái nếu cần
  };
  const handleSearchChange = (e) => {
    setSearchQuery(e.target.value); // Cập nhật nội dung tìm kiếm khi người dùng nhập
  };
  const filteredEquipmentData = equipmentList.filter(
    (Equipment) => Equipment.equipmentCode.toLowerCase().includes(searchQuery.toLowerCase()) // Tìm kiếm theo email
  );

  return (
    <>
      <Header />
      {isDataLoading ? <Preloader /> : null}
      <div className="user-select">
        <h1>Quản lí thiết bị</h1>
        <h2>Thiết bị trong hệ thống SUPER GYM</h2>

        <div className="select-search-container">
          <div className="search-container">
            <input
              type="text"
              id="searchUser"
              className="form-control"
              placeholder="Tìm kiếm theo mã thiết bị..."
              value={searchQuery} // Liên kết với state searchQuery
              onChange={handleSearchChange} // Cập nhật state khi người dùng nhập
            />
            <span className="search-icon">
              <SearchIcon />
            </span>
          </div>

          {/* <select className="form-control  form-select" id="selectRole">
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
          </select> */}
        </div>
      </div>

      <div className="table-container">
        <div className="table-wrapper">
          <div className="table-title">
            <div className="row">
              <div className="col-sm-6">
                <h2>Quản lí thiết bị</h2>
              </div>
              <div className="col-sm-6 d-flex justify-content-end">
                <button onClick={fetchImportHistory} className="btn btn-info me-2">
                  <SearchIcon /> Xem lịch sử nhập
                </button>
                <button onClick={openAddExistingEquipmentModal} className="btn btn-custom-equipment me-2">
                  <AddCircleOutlineIcon /> Nhập thiết bị có sẵn
                </button>
                <button onClick={openAddNewEquipmentModal} className="btn btn-success">
                  <AddCircleOutlineIcon /> Nhập thiết bị mới
                </button>
                {/* <button className="btn btn-exel-custom align-items-center" onClick={handleFileUpload}>
                  Nhập <FileDownloadIcon />
                </button> */}
                {/* <input className="custom-input-file" type="file" accept=".xlsx, .xls" onChange={handleFileChange} /> */}
              </div>
            </div>
          </div>

          <table className="table table-hover table-fixed">
            <thead>
              <tr>
                <th>Tên thiết bị</th>
                <th>Mã</th>
                <th>Thương hiệu</th>
                <th>Số lượng</th>
                <th>Nơi sản xuất</th>
                <th>Hạng mục</th>
                <th>Cơ sở</th>
                <th>Hành động</th>
              </tr>
            </thead>
            <tbody>
              {filteredEquipmentData.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage).map((equipment) => (
                <tr key={equipment.equipmentId}>
                  <td>{equipment.equipmentName}</td>
                  <td>{equipment.equipmentCode}</td>
                  <td>{equipment.equipmentBrand}</td>
                  <td>{equipment.equipmentQuantity}</td>
                  <td>{equipment.equipmentManufactured}</td>
                  <td>{equipment.equipmentCategoryName}</td>
                  <td>{equipment.trainingRoomName}</td>
                  <td>
                    <a href="#" onClick={() => openEditEquipmentModal(equipment)} className="edit">
                      <EditIcon />
                    </a>
                    <a href="#" onClick={() => openDeleteModal(equipment)} className="delete">
                      <DeleteIcon />
                    </a>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="clearfix-el">
            <div className="hint-text">
              Hiển thị <b>{currentEquipments.length}</b> trong <b>{equipmentList.length}</b> kết quả
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

      {/* Equipment Modal */}
      <div ref={equipmentModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">{modalTitle}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>

              <div className="modal-body">
                {!isAddingNew && !currentEquipment && (
                  <div>
                    <label className="me-2">Tìm kiếm mã thiết bị</label>
                    <div className="d-flex align-items-center mb-3">
                      <input type="text" className="form-control me-2" value={searchCode} onChange={handleSearchCodeChange} placeholder="Nhập mã thiết bị" />
                      <button type="button" className="btn btn-primary" onClick={searchEquipmentByCode}>
                        Tìm kiếm
                      </button>
                    </div>
                  </div>
                )}
                <div className="row">
                  {/* <div className={`form-group col ${currentEquipment ? "equipmentName-fix" : ""}`}> */}
                  <div className={`form-group col`}>
                    <label>
                      Tên thiết bị <span className="icon-input">(*)</span>
                    </label>
                    <input disabled={isEditingCode || isExistingEquipment} type="text" className={`form-control ${errors.equipmentName ? "is-invalid" : ""}`} name="equipmentName" value={formData.equipmentName} onChange={handleInputChange} />
                    {errors.equipmentName && <div className="invalid-feedback">{errors.equipmentName}</div>}
                  </div>

                  <div className="form-group col">
                    {/* <div className="d-flex align-items-center"> */}
                      {/* <label className="me-2"> */}
                      <label>
                        Mã thiết bị <span className="icon-input">(*)</span>
                      </label>
                      {/* {currentEquipment && !isEditingCode && (
                        <button type="button" className="edit edit-equipmentCode" onClick={handleEditCodeClick}>
                          Sửa <EditIcon style={{ fontSize: "16px" }} />
                        </button>
                      )} */}
                    {/* </div> */}
                    <input
                      type="text"
                      className={`form-control ${errors.equipmentCode ? "is-invalid" : ""}`}
                      name="equipmentCode"
                      value={formData.equipmentCode}
                      onChange={handleInputChange}
                      required
                      disabled={(currentEquipment && !isAddingNew && !isEditingCode) || isExistingEquipment} // Chỉ vô hiệu hóa khi đang chỉnh sửa thiết bị hiện có
                    />
                    {errors.equipmentCode && <div className="invalid-feedback">{errors.equipmentCode}</div>}

                    {isEditingCode && (
                      <div className="d-flex align-items-center justify-content-center mt-2">
                        <button type="button" className="btn btn-confirm-editEquipmentCode" onClick={handleConfirmCodeClick}>
                          Xác nhận
                        </button>
                        <button type="button" className="btn btn-cancel-editEquipmentCode ms-2" onClick={handleCancelEditClick}>
                          Hủy
                        </button>
                      </div>
                    )}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Giá nhập thiết bị <span className="icon-input">(*)</span>
                    </label>
                    <div className="input-group">
                      <input type="text" className={`form-control equipment-price-input ${errors.equipmentImportPrice ? "is-invalid" : ""}`} name="equipmentImportPrice" value={formData.equipmentImportPrice} onChange={handleInputChange} required disabled={isEditingCode || isExistingEquipment} />
                      <span className="input-readonly">VNĐ</span>
                      {errors.equipmentImportPrice && <div className="invalid-feedback">{errors.equipmentImportPrice}</div>}
                    </div>
                  </div>

                  <div className="form-group col">
                    <label>
                      Nhãn hiệu <span className="icon-input">(*)</span>
                    </label>
                    <input disabled={isEditingCode || isExistingEquipment} type="text" className={`form-control ${errors.equipmentBrand ? "is-invalid" : ""}`} name="equipmentBrand" value={formData.equipmentBrand} onChange={handleInputChange} required />
                    {errors.equipmentBrand && <div className="invalid-feedback">{errors.equipmentBrand}</div>}
                  </div>
                </div>
                <div className="row">
                  <div className="form-group col">
                    <label>
                      {isAddingNew ? "Số lượng nhập" : "Số lượng hiện có"} <span className="icon-input">(*)</span>
                    </label>
                    <input disabled={isEditingCode || isExistingEquipment} type="number" className={`form-control ${errors.equipmentQuantity ? "is-invalid" : ""}`} name="equipmentQuantity" value={formData.equipmentQuantity} onChange={handleInputChange} required />
                    {errors.equipmentQuantity && <div className="invalid-feedback">{errors.equipmentQuantity}</div>}
                  </div>

                  {isExistingEquipment && (
                    <div className="form-group col">
                      <label>
                        Số lượng nhập <span className="icon-input">(*)</span>
                      </label>
                      <input type="number" className="form-control" name="importQuantity" value={formData.importQuantity} onChange={handleInputChange} required />
                    </div>
                  )}
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Cơ sở <span className="icon-input">(*)</span>
                    </label>
                    <div>
                      <label>
                        <input disabled={isEditingCode || isExistingEquipment} type="radio" name="trainingRoomId" value="1" checked={formData.trainingRoomId === 1} onChange={handleRadioChange} />
                        Cơ sở 1
                      </label>
                      <label className="ms-3">
                        <input disabled={isEditingCode || isExistingEquipment} type="radio" name="trainingRoomId" value="2" checked={formData.trainingRoomId === 2} onChange={handleRadioChange} />
                        Cơ sở 2
                      </label>
                    </div>
                  </div>
                  <div className="form-group col">
                    <label>
                      Loại thiết bị <span className="icon-input">(*)</span>
                    </label>
                    <div>
                      <label>
                        <input disabled={isEditingCode || isExistingEquipment} type="radio" name="equipmentCategoryId" value="1" checked={formData.equipmentCategoryId === 1} onChange={handleRadioChange} />
                        Máy
                      </label>
                      <label className="ms-3">
                        <input disabled={isEditingCode || isExistingEquipment} type="radio" name="equipmentCategoryId" value="2" checked={formData.equipmentCategoryId === 2} onChange={handleRadioChange} />
                        Vật dụng
                      </label>
                    </div>
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Kích thước thiết bị (cm) <span className="icon-input">(*)</span>
                    </label>
                    <div className="input-group">
                      <input disabled={isEditingCode || isExistingEquipment} type="text" className={`form-control equipment-size-input ${errors.equipmentSize ? "is-invalid" : ""}`} name="equipmentSize" value={formData.equipmentSize} onChange={handleInputChange} required />
                      <span className={`input-readonly input-size-readonly ${isEditingCode || isExistingEquipment ? "readonly-bg-color" : ""}`}>dài * rộng * cao</span>
                    </div>

                    {errors.equipmentSize && <div className="invalid-feedback">{errors.equipmentSize}</div>}
                  </div>
                  <div className="form-group col">
                    <label>
                      Trọng lượng tải (kg) <span className="icon-input">(*)</span>
                    </label>
                    <input disabled={isEditingCode || isExistingEquipment} type="number" className={`form-control ${errors.equipmentWeightStack ? "is-invalid" : ""}`} name="equipmentWeightStack" value={formData.equipmentWeightStack} onChange={handleInputChange} required />
                    {errors.equipmentWeightStack && <div className="invalid-feedback">{errors.equipmentWeightStack}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Chất liệu <span className="icon-input">(*)</span>
                    </label>
                    <input disabled={isEditingCode || isExistingEquipment} type="text" className={`form-control ${errors.equipmentMaterial ? "is-invalid" : ""}`} name="equipmentMaterial" value={formData.equipmentMaterial} onChange={handleInputChange} required />
                    {errors.equipmentMaterial && <div className="invalid-feedback">{errors.equipmentMaterial}</div>}
                  </div>
                  <div className="form-group col">
                    <label>
                      Nơi sản xuất <span className="icon-input">(*)</span>
                    </label>
                    <input disabled={isEditingCode || isExistingEquipment} type="text" className={`form-control ${errors.equipmentManufactured ? "is-invalid" : ""}`} name="equipmentManufactured" value={formData.equipmentManufactured} onChange={handleInputChange} required />
                    {errors.equipmentManufactured && <div className="invalid-feedback">{errors.equipmentManufactured}</div>}
                  </div>
                </div>
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeModal} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
                  Hủy
                </button>

                <button type="submit" className="btn btn-success" disabled={isSubmitDisabled}>
                  Lưu
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      {/* Import History Modal */}
      <div ref={importHistoryModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered modal-lg">
          <div className="modal-content modal-content-importEquipment">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Lịch sử nhập thiết bị</h4>
              <a type="button" className="close" onClick={closeImportHistoryModal}>
                <CloseIcon />
              </a>
            </div>
            <div className="modal-body modal-body-importEquipment">
              <table className="table table-importEquipment">
                <thead>
                  <tr>
                    <th>Ngày nhập</th>
                    <th>Tên thiết bị</th>
                    <th>Mã thiết bị</th>
                    <th>Số lượng nhập</th>
                    <th>Giá nhập (VNĐ)</th>
                    <th>Tổng tiền (VNĐ)</th>
                  </tr>
                </thead>
                <tbody>
                  {importHistoryData.map((item) => (
                    <tr key={item.importEquipmentId}>
                      <td>{`${item.importDate.date}/${item.importDate.month}/${item.importDate.year}`}</td>
                      <td>{item.equipmentName}</td>
                      <td>{item.equipmentCode}</td>
                      <td>{item.importQuantity}</td>
                      <td>{item.importPrice.toLocaleString()}</td>
                      <td>{item.importTotalPrice.toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-default" onClick={closeImportHistoryModal}>
                Đóng
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Modal Overlay */}
      <div className="modal-overlay"></div>

      {/* Delete Modal */}
      <div ref={deleteModalRef} className="modal">
        <div className="modal-dialog modal-dialog-notify">
          <div className="modal-content">
            <form id="deleteEmployeeForm" onSubmit={handleDeleteEquipment}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">Xóa thiết bị</h4>
                <a type="button" className="close" onClick={closeDeleteModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                <p>Bạn có chắc chắn muốn xóa {currentEquipment?.equipmentName}?</p>
                <p className="text-warning">
                  <small>Hành động này sẽ không được hoàn tác.</small>
                </p>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeDeleteModal}>
                  Hủy
                </button>
                <button type="sumit" className="btn btn-danger">
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

export default ManageEquipment;
