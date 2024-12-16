import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "../assets/css/common.css";
import "./EditTrainer.css";
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

const EditTrainer = () => {
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
  const [errors, setErrors] = useState({});
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 8; // Maximum 2 trainers per page

  const trainerModalRef = useRef(null);
  const addTrainToCourseModalRef = useRef(null);
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);
  const errorModalRef = useRef(null);
  const salaryModalRef = useRef(null);

  //FETCH DATA AND PRELOAD
  // useEffect(() => {
  //   const fetchTrainers = async () => {
  //     const token = localStorage.getItem("token");
  //     try {
  //       const response = await axios.get("http://localhost:5000/api/Trainer/GetAllTrainersWithOptions", {
  //         headers: { Authorization: `Bearer ${token}` },
  //       });
  //       setTrainerData(response.data);
  //     } catch (error) {
  //       console.error("Error fetching trainers:", error);
  //     } finally {
  //       setIsDataLoading(false);
  //     }
  //   };
  //   fetchTrainers();
  // }, []);

  useEffect(() => {
    const fetchUserTrainers = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Users/GetTrainerAccounts", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setTrainerUserData(response.data);
      } catch (error) {
        console.error("Error fetching trainers:", error);
      } finally {
        setIsDataLoading(false);
      }
    };
    fetchUserTrainers();
  }, []);

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
      name: trainer.name,
      gender: trainer.gender,
      dob: formattedDob,
      email: trainer.email,
      phone: trainer.phone,
      address: trainer.address,
      userId: trainer.userId,
      idCard: trainer.idCard,
      userAvatar: trainer.userAvatar,
    });
    if (trainer.userAvatar) {
      setPreviewImage(trainer.userAvatar); // Set the current avatar to preview image
    } else {
      setPreviewImage(null); // No avatar, clear the preview
    }
    setCurrentTrainer(trainer);
    trainerModalRef.current.style.display = "block";
    trainerModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const closeModal = () => {
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

      try {
        if (currentTrainer) {
          const trainerDataEdit = {
            userId: currentTrainer.userId,
            name: formData.name,
            email: formData.email,
            gender: formData.gender,
            dob: dobData,
            address: formData.address,
            phone: formData.phone,
            roleId: "string",
            userAvatar: formData.userAvatar,
            idCard: formData.idCard,
          };
          // Update trainer
          await axios.patch(`http://localhost:5000/api/Users/updatetrainer/${currentTrainer.userId}`, trainerDataEdit, {
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
    e.preventDefault(); // Ngăn hành vi mặc định của form
    // Chuẩn bị dữ liệu gửi tới API
    const dataGym = {
      trainerRentalPlanId: "string", // Bạn cần gán ID hợp lệ tại đây nếu cần
      trainerId: trainerToAddCourse.trainerId, // ID của huấn luyện viên đã chọn
      rentalOptionId: gymPackageId, // rentalOptionId là giá trị ID của gói đã chọn
    };

    const dataBoxing = {
      boxingMembershipPlanId: "string",
      boxingTrainerId: trainerToAddCourse.trainerId,
      boxingOptionId: boxingPackageId,
    };

    if ((gymPackageId != null && gymPackageId !== "") || (boxingPackageId != null && boxingPackageId !== "")) {
      const token = localStorage.getItem("token");
      if (selectedOptionType === "gym") {
        try {
          // Gửi yêu cầu API để thêm huấn luyện viên vào khóa học
          const response = await axios.post(
            "http://localhost:5000/api/trainerRentalPlan",
            dataGym, // Gửi payload
            {
              headers: { Authorization: `Bearer ${token}` }, // Thêm token vào header
            }
          );

          // Cập nhật dữ liệu khi thành công
          setTrainerData((prevData) =>
            prevData.map((trainer) =>
              trainer.trainerId === trainerToAddCourse.trainerId
                ? { ...trainer, isAssignedToCourse: true } // Đánh dấu huấn luyện viên đã được thêm vào khóa học
                : trainer
            )
          );

          // Hiển thị thông báo thành công và đóng modal
          closeModal(); // Đóng modal
          showSuccessModal("Huấn luyện viên đã được thêm vào khóa học thành công!");
        } catch (error) {
          console.error("Error adding trainer to course:", error);
          showSuccessModal("Có lỗi khi thêm huấn luyện viên vào khóa học.");
        }
      } else {
        try {
          // Gửi yêu cầu API để thêm huấn luyện viên vào khóa học
          const response = await axios.post(
            "http://localhost:5000/api/boxingMembershipPlan",
            dataBoxing, // Gửi payload
            {
              headers: { Authorization: `Bearer ${token}` }, // Thêm token vào header
            }
          );

          // Cập nhật dữ liệu khi thành công
          setTrainerData((prevData) =>
            prevData.map((trainer) =>
              trainer.trainerId === trainerToAddCourse.trainerId
                ? { ...trainer, isAssignedToCourse: true } // Đánh dấu huấn luyện viên đã được thêm vào khóa học
                : trainer
            )
          );

          // Hiển thị thông báo thành công và đóng modal
          closeModal(); // Đóng modal
          showSuccessModal("Huấn luyện viên đã được thêm vào khóa học thành công!");
        } catch (error) {
          console.error("Error adding trainer to course:", error);
          showErrorModal(error.message);
        }
      }
    } else {
      // Nếu không chọn huấn luyện viên hoặc gói, hiển thị thông báo lỗi
      showSuccessModal("Vui lòng chọn và gói!");
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
  const filteredTrainerData = trainerUserData.filter(
    (Trainer) => Trainer.name.toLowerCase().includes(searchQuery.toLowerCase()) // Tìm kiếm theo email
  );

  // const openViewSalaryModal = async (feedback) => {
  const openViewSalaryModal = () => {
    // setIsLoadingUser(true);
    // const token = localStorage.getItem("token");

    // try {
    //   const userResponse = await axios.get(
    //     `http://localhost:5000/api/Users/GetUserById/${feedback.userId}`,
    //     { headers: { Authorization: `Bearer ${token}` } }
    //   );
    //   setCurrentUserData(userResponse.data);
    //   setCurrentFeedback(feedback);
    // } catch (error) {
    //   console.error(`Error fetching user data:`, error);
    //   setCurrentUserData({ name: "Unknown", email: "Unknown" });
    // } finally {
    // setIsLoadingUser(false);
    salaryModalRef.current.style.display = "block";
    salaryModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
    // }
  };

  return (
    <>
      <Header />

      {isLoading ? <Preloader /> : <div>{/* Nội dung khác của ManageTrainer */}</div>}
      {/* <!-- ***** Preloader End ***** --> */}

      <div className="user-select">
        <h1>Sửa thông tin huấn luyện viên trong hệ thống super gym</h1>

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
                <h2>Sửa thông tin huấn luyện viên</h2>
              </div>
              {/* <div className="col-sm-6">
                <button onClick={openAddTrainerModal} className="btn btn-success">
                  <AddCircleOutlineIcon />
                  <span>Thêm mới huấn luyện viên</span>
                </button>
              </div> */}
            </div>
          </div>

          <table className="table table-hover table-fixed">
            <thead>
              <tr>
                <th className="name-el">Họ tên</th>
                <th>Giới tính</th>
                <th>Ngày sinh</th>
                <th>Email</th>
                <th>Số điện thoại</th>
                <th>Địa chỉ</th>
                {/* <th className="status-center">Trạng thái</th> */}
                {/* <th className="role-el">Vai trò</th> */}
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
                  {/* <td>{customer.gender}</td> */}
                  <td>{trainer.gender === "male" ? "Nam" : "Nữ"}</td>
                  <td>{formatDate(trainer.dob)}</td>
                  <td>{trainer.email}</td>
                  <td>{trainer.phone}</td>
                  <td>{trainer.address}</td>
                  {/* <td className={customer.status === "Hoạt động" ? "status-el-active" : "status-el-inactive"}>{customer.status}</td> */}
                  {/* <td>{customer.role}</td> */}
                  <td>
                    <a href="#" onClick={() => openEditTrainerModal(trainer)} className="edit">
                      <EditIcon />
                    </a>
                    <a href="#" onClick={() => openViewSalaryModal(trainer)} className="view">
                      <MonetizationOnIcon />
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
                    <input type="email" className={`form-control ${errors.email ? "is-invalid" : ""}`} name="email" value={formData.email} onChange={handleInputChange} required disabled />
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

      <div ref={salaryModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Lương trainer</h4>
              <a type="button" className="close" onClick={closeModal}>
                <CloseIcon />
              </a>
            </div>
            <div className="modal-body">
              {/* {isLoadingUser ? (
                <div className="text-center">
                  <div className="spinner-border" role="status">
                    <span className="visually-hidden">Loading...</span>
                  </div>
                </div>
              ) : (
                currentFeedback &&
                currentUserData && (
                  <> */}
              <p>
                <strong>Email:</strong>
              </p>
              <p>
                <strong>Tên:</strong>
              </p>
              <p>
                <strong>Số ngày checkin trong tháng:</strong>
              </p>
              <p>
                <strong>Lương cơ bản:</strong>
              </p>
              <p>
                <strong>Số ngày đã checkin trong tháng:</strong>
              </p>
              <p>
                <strong>Lương được nhận:</strong>
              </p>
              {/* </>
                )
              )} */}
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

export default EditTrainer;
