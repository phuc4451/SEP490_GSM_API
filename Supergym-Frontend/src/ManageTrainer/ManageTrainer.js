import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "../assets/css/common.css";
import "./ManageTrainer.css";
import Select from "react-select";
import SearchIcon from "@mui/icons-material/Search";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import { Delete } from "@mui/icons-material";
import MonetizationOnIcon from "@mui/icons-material/MonetizationOn";
import VisibilityIcon from "@mui/icons-material/Visibility";

const ManageTrainer = () => {
  const [trainerDataList, setTrainerDataList] = useState([]);
  const [searchQuery, setSearchQuery] = useState("");

  const [selectedOptionType, setSelectedOptionType] = useState("");
  const [selectedRentalOption, setSelectedRentalOption] = useState(""); // State cho lựa chọn Gym
  const [selectedBoxingOption, setSelectedBoxingOption] = useState(""); // State cho lựa chọn Boxing

  const [gymPackageId, setGymPackageId] = useState(""); // State cho lựa chọn Boxing
  const [boxingPackageId, setBoxingPackageId] = useState(""); // State cho lựa chọn Boxing
  const [errorMessage, setErrorMessage] = useState("");

  //PRELOAD
  const [trainerData, setTrainerData] = useState([]);
  const [trainerUserData, setTrainerUserData] = useState([]);
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
    trainerId: "",
    userId: "",
    name: "",
    isTrainerGym: true,
    isTrainerBoxing: true,
    bio: "",
    specialization: "",
  });

  const [formSalary, setFormSalary] = useState({
    assignmentId: "string",
    staffId: "",
    shiftId: "",
    configurationId: "",
    assignedDate: "",
    endDate: "",
  });
  const [errors, setErrors] = useState({});
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 8; // Maximum 2 trainers per page

  const trainerModalRef = useRef(null);
  const addTrainToCourseModalRef = useRef(null);
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);
  const errorModalRef = useRef(null);
  const AssignSalaryAndShiftModalRef = useRef(null);
  const [isSaving, setIsSaving] = useState(false);
  const [shifts, setShifts] = useState([]);
  const [salaryConfigs, setSalaryConfigs] = useState([]);
  const [currentTrainerForSalary, setCurrentTrainerForSalary] = useState(null);
  const [salaryPeriod, setSalaryPeriod] = useState({
    fromDate: "",
    toDate: "",
  });
  const [salaryReport, setSalaryReport] = useState(null);
  const [isCalculating, setIsCalculating] = useState(false);
  const salaryModalRef = useRef(null);

  useEffect(() => {
    const fetchShifts = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Salary/GetShifts", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setShifts(response.data);
      } catch (error) {
        console.error("Error fetching shifts:", error);
      }
    };

    const fetchSalaryConfigs = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Salary/GetSalaryConfigs", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setSalaryConfigs(response.data);
      } catch (error) {
        console.error("Error fetching salary configurations:", error);
      }
    };

    fetchShifts();
    fetchSalaryConfigs();
  }, []);

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

  // useEffect(() => {
  //   const fetchUserTrainers = async () => {
  //     const token = localStorage.getItem("token");
  //     try {
  //       const response = await axios.get("http://localhost:5000/api/Trainer/GetTrainerAccounts", {
  //         headers: { Authorization: `Bearer ${token}` },
  //       });
  //       setTrainerUserData(response.data);
  //     } catch (error) {
  //       console.error("Error fetching trainers:", error);
  //     } finally {
  //       setIsDataLoading(false);
  //     }
  //   };
  //   fetchUserTrainers();
  // }, []);

  useEffect(() => {
    const fetchRentalOptions = async () => {
      const token = localStorage.getItem("token");
      try {
        const rentalResponse = await axios.get("http://localhost:5000/api/RentalOption", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setRentalOptions(rentalResponse.data); // Set rental options
      } catch (error) {
        console.error("Error fetching rental options:", error);
      }
    };

    fetchRentalOptions();
  }, []); // Runs once on mount

  useEffect(() => {
    const fetchBoxingOptions = async () => {
      const token = localStorage.getItem("token");
      try {
        const boxingResponse = await axios.get("http://localhost:5000/api/BoxingOption", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setBoxingOptions(boxingResponse.data); // Set boxing options
      } catch (error) {
        console.error("Error fetching boxing options:", error);
      }
    };

    fetchBoxingOptions();
  }, []); // Runs once on mount

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

  const openAddShiftAndSalaryModal = (trainer) => {
    setCurrentTrainerForSalary(trainer); // Store the current trainer
    setFormSalary({
      assignmentId: "string",
      staffId: "",
      shiftId: "",
      configurationId: "",
      assignedDate: "",
      endDate: "",
    });

    setErrors({});
    AssignSalaryAndShiftModalRef.current.style.display = "block";
    AssignSalaryAndShiftModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const openAddTrainerModal = () => {
    setFormData({
      email: "",
      isTrainerGym: true,
      isTrainerBoxing: false,
      name: "",
      gender: "male",
      dob: "",
      address: "",
      phone: "",
      userAvatar: "",
      idCard: "",
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
      email: trainer.email,
      isTrainerBoxing: trainer.isTrainerBoxing,
      isTrainerGym: trainer.isTrainerGym,
      name: trainer.name,
      gender: trainer.gender,
      dob: formattedDob,
      address: trainer.address,
      phone: trainer.phone,
      userAvatar: trainer.userAvatar,
      idCard: trainer.idCard,
    });
    setCurrentTrainer(trainer);
    trainerModalRef.current.style.display = "block";
    trainerModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const closeModal = () => {
    AssignSalaryAndShiftModalRef.current.style.display = "none";
    AssignSalaryAndShiftModalRef.current.classList.remove("active");
    salaryModalRef.current.style.display = "none";
    salaryModalRef.current.classList.remove("active");
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
    const { name, value } = e.target;

    setFormData({
      ...formData,
      [name]: value,
    });

    // Trigger validation for the specific field
    validateField(name, value);
  };

  const validateField = (name, value) => {
    const newErrors = { ...errors };

    switch (name) {
      case "name":
        if (value.length < 5 || value.length > 30) {
          newErrors.name = "Họ tên phải từ 5 đến 30 ký tự.";
        } else {
          delete newErrors.name; // Clear error if valid
        }
        break;
      case "dob":
        const dob = new Date(value);
        const currentDate = new Date();
        if (dob >= currentDate) {
          newErrors.dob = "Ngày tháng năm sinh phải nhỏ hơn ngày hiện tại.";
        } else {
          delete newErrors.dob; // Clear error if valid
        }
        break;
      case "email":
        const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test(value)) {
          newErrors.email = "Vui lòng nhập địa chỉ email hợp lệ.";
        } else {
          delete newErrors.email; // Clear error if valid
        }
        break;
      case "phone":
        if (value.length < 9 || value.length > 11 || !/^\d+$/.test(value)) {
          newErrors.phone = "Số điện thoại phải từ 9 đến 11 chữ số.";
        } else {
          delete newErrors.phone; // Clear error if valid
        }
        break;
      case "address":
        if (!value) {
          newErrors.address = "Địa chỉ không được để trống.";
        } else {
          delete newErrors.address; // Clear error if valid
        }
        break;
      case "idCard":
        if (!value) {
          newErrors.idCard = "Số căn cước không được để trống.";
        } else {
          delete newErrors.idCard; // Clear error if valid
        }
        break;
      case "gender":
        if (!value) {
          newErrors.gender = "Vui lòng chọn giới tính.";
        } else {
          delete newErrors.gender; // Clear error if valid
        }
        break;
      default:
        break;
    }

    setErrors(newErrors);
  };

  const validateForm = () => {
    const newErrors = {};

    // Check all fields here
    if (formData.name.length < 5 || formData.name.length > 30) {
      newErrors.name = "Họ tên phải từ 5 đến 30 ký tự.";
    }

    const dob = new Date(formData.dob);
    const currentDate = new Date();
    if (dob >= currentDate) {
      newErrors.dob = "Ngày tháng năm sinh phải nhỏ hơn ngày hiện tại.";
    }

    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailPattern.test(formData.email)) {
      newErrors.email = "Vui lòng nhập địa chỉ email hợp lệ.";
    }

    if (formData.phone.length < 9 || formData.phone.length > 11 || !/^\d+$/.test(formData.phone)) {
      newErrors.phone = "Số điện thoại phải từ 9 đến 11 chữ số.";
    }

    if (!formData.address) {
      newErrors.address = "Địa chỉ không được để trống.";
    }

    if (!formData.idCard) {
      newErrors.idCard = "Số căn cước không được để trống.";
    }

    if (!formData.gender) {
      newErrors.gender = "Vui lòng chọn giới tính.";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0; // If no errors, form is valid
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

  // Hàm để mở modal thông báo
  const showErrorModal = (message) => {
    setErrorMessage(message); // Cập nhật thông báo
    errorModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  // Hàm để đóng modal thông báo
  const closeErrorModal = () => {
    errorModalRef.current.style.display = "none"; // Ẩn modal
    // document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    // window.location.reload();
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
      // if (!currentTrainer) {
      //   trainerData.gymMembershipId = "string"; // Include gymMembershipId for new trainers
      //   trainerData.password = formData.password; // Include password for new trainers
      // }

      try {
        if (currentTrainer) {
          // Update trainer
          await axios.patch(`http://localhost:5000/api/Trainer/updateTrainer/${currentTrainer.trainerId}`, trainerData, {
            headers: { Authorization: `Bearer ${token}` },
          });

          // Update trainer data in the list
          setTrainerDataList((prevData) => prevData.map((trainer) => (trainer.userId === currentTrainer.userId ? { ...trainer, ...trainerData } : trainer)));
        } else {
          const addTrainerData = {
            email: formData.email,
            isGymer: formData.isTrainerGym,
            isBoxer: formData.isTrainerBoxing,
            name: formData.name,
            gender: formData.gender,
            dob: dobData,
            address: formData.address,
            phone: formData.phone,
            userAvatar: formData.userAvatar,
            idCard: formData.idCard,
          };
          // Add new trainer
          const response = await axios.post("http://localhost:5000/api/Users/addTrainer", addTrainerData, {
            headers: { Authorization: `Bearer ${token}` },
          });

          setTrainerDataList([...trainerDataList, response.data]);
        }

        // Reset form and close modal
        setFormData({
          email: "",
          isTrainerGym: true,
          isTrainerBoxing: false,
          name: "",
          gender: "male",
          dob: "",
          address: "",
          phone: "",
          userAvatar: "",
          idCard: "",
        });
        closeModal();
        showSuccessModal("Huấn luyện viên được lưu thành công");
      } catch (error) {
        console.error("Error saving trainer:", error);
        showErrorModal(error.message);
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
    e.preventDefault();

    // Validate package selection first
    if (!gymPackageId && !boxingPackageId) {
      showErrorModal("Vui lòng chọn gói tập!");
      return;
    }

    const token = localStorage.getItem("token");

    try {
      let response;

      if (selectedOptionType === "gym") {
        const dataGym = {
          trainerRentalPlanId: "string",
          trainerId: trainerToAddCourse.trainerId,
          rentalOptionId: gymPackageId,
        };

        response = await axios.post("http://localhost:5000/api/trainerRentalPlan", dataGym, {
          headers: { Authorization: `Bearer ${token}` },
        });
      } else {
        const dataBoxing = {
          boxingMembershipPlanId: "string",
          boxingTrainerId: trainerToAddCourse.trainerId,
          boxingOptionId: boxingPackageId,
        };

        response = await axios.post("http://localhost:5000/api/boxingMembershipPlan", dataBoxing, {
          headers: { Authorization: `Bearer ${token}` },
        });
      }

      // Nếu thành công
      setTrainerData((prevData) => prevData.map((trainer) => (trainer.trainerId === trainerToAddCourse.trainerId ? { ...trainer, isAssignedToCourse: true } : trainer)));

      closeModal();
      showSuccessModal("Huấn luyện viên đã được thêm vào khóa học thành công!");
    } catch (error) {
      console.error("Chi tiết lỗi:", error);

      // Xử lý các loại lỗi khác nhau
      if (error.response) {
        // Server trả về response với status code nằm ngoài range 2xx
        const errorMessage = error.response.data.message || error.response.data || "Có lỗi xảy ra từ server";
        showErrorModal(`Lỗi: ${error.response.status} - ${errorMessage}`);

        console.log("Status:", error.response.status);
        console.log("Data:", error.response.data);
        console.log("Headers:", error.response.headers);
      } else if (error.request) {
        // Request được gửi nhưng không nhận được response
        showErrorModal("Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng.");
        console.log("Request error:", error.request);
      } else {
        // Có lỗi khi setting up request
        showErrorModal(`Lỗi: ${error.message}`);
        console.log("Error:", error.message);
      }

      // Log thêm config nếu cần debug
      console.log("Config:", error.config);
    }
  };

  const handleImageChange = (e) => {
    const file = e.target.files[0];
    if (file) {
      // Use FileReader to convert image to base64
      const reader = new FileReader();

      reader.onloadend = () => {
        // Extract base64 data without the "data:image/jpeg;base64," prefix
        const base64String = reader.result.split(",")[1]; // Remove the prefix

        // Set preview image for display (include the full base64 string with prefix)
        setPreviewImage(reader.result.split(",")[1]); // Set only the image data for preview

        // Store base64 image without the prefix in formData
        setFormData({
          ...formData,
          userAvatar: base64String, // Store base64 image without the prefix in formData
        });
      };

      reader.readAsDataURL(file); // Convert the file to base64
    }
  };

  const handleCheckboxChange = (e) => {
    const { name, checked } = e.target;

    // Đếm số checkbox được chọn hiện tại
    const selectedCount = Object.values(formData).filter(Boolean).length;

    // Nếu checkbox cuối cùng được bỏ chọn, ngăn hành động
    if (!checked && selectedCount === 1) {
      alert("Phải có ít nhất một loại huấn luyện viên được chọn!");
      return;
    }

    // Cập nhật trạng thái nếu điều kiện không vi phạm
    setFormData({
      ...formData,
      [name]: checked,
    });
  };

  const handleOptionTypeChange = (e) => {
    setSelectedOptionType(e.target.value);
    setSelectedRentalOption(""); // Reset Gym option if switching to Boxing
    setSelectedBoxingOption(""); // Reset Boxing option if switching to Gym
  };

  const onChangeSelectedGymPackage = (event) => {
    const selectedGymPackageId = event.target.value;
    setGymPackageId(selectedGymPackageId);
  };

  const onChangeSelectedBoxingPackage = (event) => {
    const selectedBoxingPackageId = event.target.value;
    setBoxingPackageId(selectedBoxingPackageId);
  };

  const handleSearchChange = (e) => {
    setSearchQuery(e.target.value); // Cập nhật nội dung tìm kiếm khi người dùng nhập
  };
  const filteredTrainerData = trainerData.filter(
    (Trainer) => Trainer.name.toLowerCase().includes(searchQuery.toLowerCase()) // Tìm kiếm theo email
  );

  const handleSalaryInputChange = (e) => {
    const { name, value } = e.target;
    setFormSalary({
      ...formSalary,
      [name]: value,
    });
    // validateSalaryField(name, value);
  };

  const handleAssign = async (e) => {
    e.preventDefault();
    const token = localStorage.getItem("token");
    setIsSaving(true);
  
    try {
      const assignedDate = new Date();
      assignedDate.setHours(0, 0, 0, 0);
      assignedDate.setMinutes(assignedDate.getMinutes() - assignedDate.getTimezoneOffset());
  
      const endDate = new Date(formSalary.endDate);
      endDate.setHours(23, 59, 59, 999);
      endDate.setMinutes(endDate.getMinutes() - endDate.getTimezoneOffset());
  
      // Log the current trainer to verify the trainerId
      console.log('Current trainer for salary:', currentTrainerForSalary);
  
      const assignmentData = {
        assignmentId: "string",
        trainerId: currentTrainerForSalary.trainerId, // Make sure this exists
        // staffId: "string",
        // shiftId: formSalary.shiftId,
        configurationId: formSalary.configurationId,
        // assignedDate: assignedDate.toISOString(),
        // endDate: endDate.toISOString(),
      };
  
      console.log("Assignment data:", assignmentData); // Log the data being sent
  
      const response = await axios.post("http://localhost:5000/api/Salary/AssignTrainerSalaryConfig", assignmentData, {
        headers: { Authorization: `Bearer ${token}` },
      });
  
      closeModal();
      showSuccessModal("thêm ca làm việc và mức lương thành công");
    } catch (error) {
      if (error.response && error.response.status === 409) {
        showErrorModal(error.response.data);
      } else {
        console.error("Error assigning shift and salary:", error);
        showErrorModal("Có lỗi xảy ra khi thêm ca làm việc");
      }
    } finally {
      setIsSaving(false);
    }
  };

  const openViewSalaryModal = (trainer) => {
    setCurrentTrainerForSalary(trainer);
    setSalaryReport(null);
    setSalaryPeriod({ fromDate: "", toDate: "" });
    salaryModalRef.current.style.display = "block";
    salaryModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const handleSalaryPeriodChange = (e) => {
    const { name, value } = e.target;
    setSalaryPeriod((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const calculateSalary = async (e) => {
    e.preventDefault();
    setIsCalculating(true);
    const token = localStorage.getItem("token");

    // Convert dates to ISO string and log for checking
    const fromDateISO = new Date(salaryPeriod.fromDate).toISOString();
    const toDateISO = new Date(salaryPeriod.toDate).toISOString();

    try {
      const requestBody = {
        reportId: "",
        trainerId: currentTrainerForSalary?.trainerId || "",
        fullName: "string",
        totalShifts: 0,
        totalSlots: 0,
        lateCount: 0,
        absenceCount: 0,
        totalFines: 0,
        finalSalary: 0,
        isBilled: true,
        fromDate: fromDateISO,
        toDate: toDateISO,
        staffId: "string",
      };

      const response = await axios.post("http://localhost:5000/api/Salary/CalculateTrainerSalary", requestBody, {
        headers: { Authorization: `Bearer ${token}` },
      });
      setSalaryReport(response.data);
    } catch (error) {
      console.error("Error calculating salary:", error);
      showErrorModal("Có lỗi xảy ra khi tính lương");
    } finally {
      setIsCalculating(false);
    }
  };
  return (
    <>
      <Header />

      {isLoading ? <Preloader /> : <div>{/* Nội dung khác của ManageTrainer */}</div>}
      {/* <!-- ***** Preloader End ***** --> */}

      <div className="user-select">
        <h1>Thêm huấn luyện viên trong hệ thống super gym</h1>

        <div className="select-search-container">
          <div className="search-container">
            <input
              type="text"
              id="searchUser"
              className="form-control"
              placeholder="Tìm kiếm theo tên..."
              value={searchQuery} // Liên kết với state searchQuery
              onChange={handleSearchChange} // Cập nhật state khi người dùng nhập
            />
            <span className="search-icon">
              <SearchIcon />
            </span>
          </div>
        </div>
      </div>

      <div className="table-container">
        <div className="table-wrapper">
          <div className="table-title">
            <div className="row">
              <div className="col-sm-6">
                <h2>Thêm mới/Thêm huấn luyện viên vào gói</h2>
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
                <th className="name-el">Tên huấn luyện viên</th>
                <th>Loại huấn luyện viên</th>
                <th>Chuyên môn</th>
                <th className="action-el">Hành động</th>
              </tr>
            </thead>
            <tbody>
              {filteredTrainerData.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage).map((trainer, index) => (
                <tr key={index}>
                  <td>
                    <img src={`data:image/jpeg;base64,${trainer.userAvatar}`} className="customer-avatar" />
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
                    {/* <a href="#" onClick={() => openEditTrainerModal(trainer)} className="edit">
                      <EditIcon />
                    </a>
                    <a href="#" onClick={() => openDeleteModal(trainer)} className="delete">
                      <DeleteIcon />
                    </a> */}
                    <td>
                      <a href="#" onClick={() => openEditTrainerModal(trainer)} className="edit">
                        <EditIcon />
                      </a>
                      <a href="#" onClick={() => openViewSalaryModal(trainer)} className="view">
                        <VisibilityIcon />
                      </a>
                      <a href="#" onClick={() => openAddShiftAndSalaryModal(trainer)} className="money-icon">
                        <MonetizationOnIcon />
                      </a>
                    </td>
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

      {/*Add new Trainer Modal */}
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
                    <input type="text" className={`form-control ${errors.name ? "is-invalid" : ""}`} name="name" value={formData.name} onChange={handleInputChange} required />
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Ngày tháng năm sinh <span className="icon-input">(*)</span>
                    </label>
                    <input type="date" className={`form-control ${errors.dob ? "is-invalid" : ""}`} name="dob" value={formData.dob} onChange={handleInputChange} required />
                    {errors.dob && <div className="error-message">{errors.dob}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Email <span className="icon-input">(*)</span>
                    </label>
                    <input type="email" className={`form-control ${errors.email ? "is-invalid" : ""}`} name="email" value={formData.email} onChange={handleInputChange} required />
                    {errors.email && <div className="error-message">{errors.email}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Số điện thoại <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${errors.phone ? "is-invalid" : ""}`} name="phone" value={formData.phone} onChange={handleInputChange} required />
                    {errors.phone && <div className="error-message">{errors.phone}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Ảnh đại diện</label>
                    <input type="file" className="form-control" onChange={handleImageChange} accept="image/*" ref={fileInputRef} />
                  </div>

                  {previewImage && (
                    <div className="form-group col">
                      <img src={`data:image/jpeg;base64,${previewImage}`} alt="Ảnh xem trước" className="preview-image" />
                    </div>
                  )}
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Địa chỉ <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${errors.address ? "is-invalid" : ""}`} name="address" value={formData.address} onChange={handleInputChange} required />
                    {errors.address && <div className="error-message">{errors.address}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Số căn cước <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${errors.idCard ? "is-invalid" : ""}`} name="idCard" value={formData.idCard} onChange={handleInputChange} required />
                    {errors.idCard && <div className="error-message">{errors.idCard}</div>}
                  </div>
                </div>

                {/* Gym and Boxing checkboxes */}
                <div className="row">
                  <div className="form-group col">
                    <label>Loại huấn luyện viên</label>
                    <div className="checkbox-group">
                      <div>
                        <input type="checkbox" id="isTrainerGym" name="isTrainerGym" checked={formData.isTrainerGym} onChange={handleCheckboxChange} />
                        <label htmlFor="isTrainerGym">Gym</label>

                        <input className="ms-3" type="checkbox" id="isTrainerBoxing" name="isTrainerBoxing" checked={formData.isTrainerBoxing} onChange={handleCheckboxChange} />
                        <label htmlFor="isTrainerBoxing">Boxing</label>
                      </div>
                    </div>
                  </div>

                  <div className="form-group col">
                    <label>Giới tính</label>
                    <div className="radio-group-trainer">
                      <label>
                        <input type="radio" name="gender" value="male" checked={formData.gender === "male"} onChange={handleInputChange} />
                        Nam
                      </label>
                      <label>
                        <input className="ms-3" type="radio" name="gender" value="female" checked={formData.gender === "female"} onChange={handleInputChange} />
                        Nữ
                      </label>
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
                      Họ tên <span className="icon-input"></span>
                    </label>
                    <input type="text" className="form-control" name="name" value={formData.name || ""} onChange={handleInputChange} required disabled />
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>
                  <div className="form-group col">
                    <label>Loại huấn luyện viên</label>
                    <div className="checkbox-group">
                      <div>
                        <input disabled type="checkbox" id="isTrainerGym" name="isTrainerGym" checked={formData.isTrainerGym} onChange={handleCheckboxChange} />
                        <label htmlFor="isTrainerGym">Gym</label>

                        <input disabled className="ms-3" type="checkbox" id="isTrainerBoxing" name="isTrainerBoxing" checked={formData.isTrainerBoxing} onChange={handleCheckboxChange} />
                        <label htmlFor="isTrainerBoxing">Boxing</label>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Chọn gói</label>
                    <div>
                      <label>
                        <input type="radio" name="optionType" value="gym" checked={selectedOptionType === "gym"} onChange={handleOptionTypeChange} />
                        Gym
                      </label>
                      <label className="ms-3">
                        <input type="radio" name="optionType" value="boxing" checked={selectedOptionType === "boxing"} onChange={handleOptionTypeChange} />
                        Boxing
                      </label>
                    </div>
                  </div>
                </div>

                {selectedOptionType === "gym" && (
                  <div className="row">
                    <div className="form-group col">
                      <label>Chọn gói Gym</label>
                      <select
                        className="form-control form-select custom-select"
                        name="option"
                        onChange={onChangeSelectedGymPackage} // Cập nhật giá trị cho gói Gym
                        required
                      >
                        <option value="">-- Chọn gói Gym --</option>
                        {rentalOptions.map((item) => (
                          <option key={item.rentalOptionId} value={item.rentalOptionId}>
                            {item.description}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>
                )}
                {selectedOptionType === "boxing" && (
                  <div className="row">
                    <div className="form-group col">
                      <label>Chọn gói Boxing</label>
                      <select
                        className="form-control form-select"
                        name="option"
                        onChange={onChangeSelectedBoxingPackage} // Cập nhật giá trị cho gói Boxing
                        required
                      >
                        <option value="">-- Chọn gói Boxing --</option>
                        {boxingOptions.map((item) => (
                          <option key={item.boxingOptionId} value={item.boxingOptionId}>
                            {item.description}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>
                )}
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

      <div ref={AssignSalaryAndShiftModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="salaryForm" onSubmit={handleAssign}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">Thêm ca làm việc và lương</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* <div className="row">
                  <div className="form-group col">
                    <label>
                      Chọn ngày kết thúc <span className="icon-input">(*)</span>
                    </label>
                    <input type="date" className={`form-control ${errors.endDate ? "is-invalid" : ""}`} name="endDate" value={formSalary.endDate} onChange={handleSalaryInputChange} min={new Date().toISOString().split("T")[0]} required />
                    {errors.endDate && <div className="error-message">{errors.endDate}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Chọn ca làm việc <span className="icon-input">(*)</span>
                    </label>
                    <select className={`form-control ${errors.shiftId ? "is-invalid" : ""}`} name="shiftId" value={formSalary.shiftId} onChange={handleSalaryInputChange} required>
                      <option value="">-- Chọn ca làm việc --</option>
                      {shifts.map((shift) => (
                        <option key={shift.shiftId} value={shift.shiftId}>
                          {shift.shiftName} ({new Date(shift.startTime).toLocaleTimeString()} - {new Date(shift.endTime).toLocaleTimeString()}) - {shift.location}
                        </option>
                      ))}
                    </select>
                    {errors.shiftId && <div className="error-message">{errors.shiftId}</div>}
                  </div>
                </div> */}

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Chọn cấu hình lương <span className="icon-input">(*)</span>
                    </label>
                    <select className={`form-control ${errors.configurationId ? "is-invalid" : ""}`} name="configurationId" value={formSalary.configurationId} onChange={handleSalaryInputChange} required>
                      <option value="">-- Chọn cấu hình lương --</option>
                      {salaryConfigs
                        .filter((config) => config.perSlotSalary > 0) // Thay đổi từ perShiftSalary sang perSlotSalary
                        .map((config) => (
                          <option key={config.configurationId} value={config.configurationId}>
                            Lương cơ bản: {config.baseSalary.toLocaleString()}đ/tháng - Lương theo buổi: {config.perSlotSalary.toLocaleString()}đ/buổi - Phạt đi muộn: {config.finePerLate.toLocaleString()}đ
                          </option>
                        ))}
                    </select>
                    {errors.configurationId && <div className="error-message">{errors.configurationId}</div>}
                  </div>
                </div>
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeModal} disabled={isSaving}>
                  Hủy
                </button>
                <button type="submit" className="btn btn-success" disabled={isSaving}>
                  {isSaving ? "Đang lưu..." : "Lưu"}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      <div ref={salaryModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Tính lương nhân viên</h4>
              <a type="button" className="close" onClick={closeModal}>
                <CloseIcon />
              </a>
            </div>
            <div className="modal-body">
              <form onSubmit={calculateSalary}>
                <div className="row mb-3">
                  <div className="form-group col">
                    <label>Từ ngày</label>
                    <input type="date" className="form-control" name="fromDate" value={salaryPeriod.fromDate} onChange={handleSalaryPeriodChange} required />
                  </div>
                  <div className="form-group col">
                    <label>Đến ngày</label>
                    <input type="date" className="form-control" name="toDate" value={salaryPeriod.toDate} onChange={handleSalaryPeriodChange} required />
                  </div>
                </div>
                <button type="submit" className="btn btn-primary w-100 mb-3" disabled={isCalculating}>
                  {isCalculating ? "Đang tính..." : "Tính lương"}
                </button>
              </form>

              {salaryReport && (
                <div className="salary-report mt-4">
                  <h5>Báo cáo lương</h5>
                  <p>
                    <strong>Họ và tên:</strong> {salaryReport.fullName}
                  </p>
                  <p>
                    <strong>Tổng số ca:</strong> {salaryReport.totalShifts}
                  </p>
                  <p>
                    <strong>Số lần đi muộn:</strong> {salaryReport.lateCount}
                  </p>
                  <p>
                    <strong>Số lần vắng mặt:</strong> {salaryReport.absenceCount}
                  </p>
                  <p>
                    <strong>Tổng tiền phạt:</strong> {salaryReport.totalFines.toLocaleString()}đ
                  </p>
                  <p>
                    <strong>Lương cuối cùng:</strong> {salaryReport.finalSalary.toLocaleString()}đ
                  </p>
                </div>
              )}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={closeModal}>
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
              <p>{errorMessage}</p> {/* Hiển thị lỗi từ state */}
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

export default ManageTrainer;
