import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import CloseIcon from "@mui/icons-material/Close";
import "bootstrap/dist/css/bootstrap.min.css";
// import "./BookTrainerForm.css";
import Header from "../Header/Header.js";
import LoadingSpinner from "../utils/LoadingOverlay";

const ManageSchedule = () => {
  const [trainerType, setTrainerType] = useState(""); // State cho loại huấn luyện viên
  const [selectedOption, setSelectedOption] = useState(""); // State cho gói đã chọn
  const BookTrainer = useRef(null);
  const [trainerToAddCourse, setTrainerToAddCourse] = useState(null); // for deletion
  const [successMessage, setSuccessMessage] = useState("");
  const successModalRef = useRef(null);
  const showQrPicture = useRef(null);
  const [formData, setFormData] = useState({
    isTrainerGym: true,
    isTrainerBoxing: true,
  });

  const [selectedPackages, setSelectedPackages] = useState([]); // Dữ liệu các gói
  const [trainers, setTrainers] = useState([]); // Store trainers data
  const [showBoxingOptions, setShowBoxingOptions] = useState(false); // For showing boxing-specific radio buttons
  const [timeSlots, setTimeSlots] = useState([]); // State to store time slots
  const [trainersData, setTrainersData] = useState([]); // Store full trainer data
  const [packagesWithTrainers, setPackagesWithTrainers] = useState([]);
  const [checkDetailPack, setCheckDetailPack] = useState(null);
  const [showSessionCount, setShowSessionCount] = useState(null); // Track if the session count input should be shown
  const [memberCount, setMemberCount] = useState(1);
  const [qrDataUrl, setQrDataUrl] = useState(""); // State to store the QR code data URL
  const [showQR, setShowQR] = useState(true);

  const [searchEmail, setSearchEmail] = useState("");
  const [courseData, setCourseData] = useState([]);
  const [courseDetail, setCourseDetail] = useState([]);
  const [selectedCourseId, setSelectedCourseId] = useState("");
  const [selectedTrainerId, setSelectedTrainerId] = useState("");
  const [selectedTimeslotId, setSelectedTimeSlotId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const errorModalRef = useRef(null);
  const [formCourseData, setFormCourseData] = useState({
    userId: "",
    registrationId: "",
    registrationType: "",
    description: "",
  });
  const [isEmailValid, setIsEmailValid] = useState(false);
  const [errors, setErrors] = useState({
    email: "",
  });
  const [isFetching, setIsFetching] = useState(false);
  const [hasSearched, setHasSearched] = useState(false);

  useEffect(() => {
    openBookTrainerModal(); // Gọi hàm mở modal khi trang load
  }, []); // Chạy khi component mount lần đầu

  const fetchTimeSlots = async () => {
    try {
      const response = await axios.get("http://localhost:5000/api/schedule/timeSlots");
      if (response.data && Array.isArray(response.data)) {
        setTimeSlots(response.data); // Set time slots data
      } else {
        console.error("Invalid response format for time slots");
      }
    } catch (error) {
      console.error("Error fetching time slots:", error);
    }
  };
  useEffect(() => {
    fetchTimeSlots(); // Fetch time slots when component mounts
  }, []);

  const handleInputChange = (e) => {
    const { name, value } = e.target;

    // Update form data
    if (name === "boxingOption") {
      // Update the isMonWedFri state based on the selected radio button value
      setFormData({
        ...formData,
        isMonWedFri: value === "true", // Sets isMonWedFri to true or false based on the radio selection
      });
    } else {
      setFormData({
        ...formData,
        [name]: value,
      });
    }
    // When radio button is selected, fetch corresponding packages
    if (name === "trainerType") {
      setTrainerType(value); // Update trainerType state
      setSelectedOption(""); // Reset selected package
      setTrainers([]); // Clear trainers list before fetching new trainers
      setShowBoxingOptions(value === "Boxing"); // Show boxing options if "Boxing" is selected
      fetchPackages(value); // Fetch packages based on selected trainer type
    }
  };

  const fetchPackages = async (type) => {
    try {
      const token = localStorage.getItem("token");
      if (!token) {
        console.error("No token found in localStorage");
        return;
      }

      const response = await axios.get(`http://localhost:5000/api/PackagesAndTrainers?type=${type}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.data && Array.isArray(response.data.packages)) {
        setSelectedPackages(response.data.packages);

        const updatedPackagesWithTrainers = await Promise.all(
          response.data.packages.map(async (packageItem) => {
            const trainers = await fetchTrainersByPackage(packageItem.packageId);
            return {
              ...packageItem, // Add the package details
              trainers, // Add trainers for this package
            };
          }) // <-- This is where the parentheses were missing
        );

        setPackagesWithTrainers(updatedPackagesWithTrainers); // Save the full response data (packages + trainers)
      } else {
        console.error("No valid packages array in response");
      }
    } catch (error) {
      console.error("Error fetching packages:", error);
    }
  };

  const fetchTrainersByPackage = async (packageId) => {
    try {
      const token = localStorage.getItem("token");
      if (!token) {
        console.error("No token found in localStorage");
        return;
      }

      // Determine the type of package (Gym or Boxing)
      const optionType = trainerType === "TrainerRental" ? "rentalOptionId" : "boxingOptionId";
      const url = `http://localhost:5000/api/PackagesAndTrainers/trainers-by-option?${optionType}=${packageId}`;

      const response = await axios.get(url, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });
      setCheckDetailPack(response.data[0]);

      if (response.data && Array.isArray(response.data)) {
        // Store the full response data (this will contain all trainer details)
        setTrainersData(response.data);

        // Process and store only relevant trainer information
        setTrainers(
          response.data.map((trainer) => ({
            trainerId: trainer.trainerId,
            name: trainer.name,
            specialization: trainer.specialization,
            userId: trainer.userId,
            isMonthlyPackage: trainer.isMonthlyPackage, // Ensure isMonthlyPackage is captured
            minSessions: trainer.minSessions,
            maxSessions: trainer.maxSessions,
            memberCount: trainer.memberCount,
            trainerRentalPlanId: trainer.trainerRentalPlanId,
            boxingMembershipPlanId: trainer.boxingMembershipPlanId,
          }))
        );
      } else {
        console.error("No valid trainers array in response");
      }
    } catch (error) {
      console.error("Error fetching trainers:", error);
    }
  };

  const handlePackageChange = (e) => {
    const selectedPackageId = e.target.value;
    setSelectedOption(selectedPackageId); // Update selected package state

    // Find the selected package by packageId
    const selectedPackage = selectedPackages.find((pkg) => pkg.packageId === selectedPackageId);

    // Fetch trainers based on the selected package
    fetchTrainersByPackage(selectedPackageId);
  };

  // // useEffect để theo dõi sự thay đổi của checkIsMonthly
  useEffect(() => {
    // Check if the trainerType is Gym or Boxing to control the display of the session count field
    if (trainerType === "TrainerRental") {
      setShowSessionCount(true); // Show session count input for Gym
    } else if (trainerType === "Boxing") {
      setShowSessionCount(false); // Hide session count input for Boxing
      setFormData((prevData) => ({
        ...prevData,
        sessionCount: 1, // Automatically set sessionCount to 1 for Boxing
      }));
    }
  }, [trainerType]); // This effect depends on the trainerType

  useEffect(() => {
    if (checkDetailPack && checkDetailPack.memberCount) {
      console.log("Member count changed:", checkDetailPack.memberCount);
      // Bạn có thể thêm logic xử lý sau khi memberCount thay đổi ở đây nếu cần
    }
  }, [checkDetailPack]); // Chạy khi checkDetailPack thay đổi

  const renderEmailInputs = () => {
    if (selectedOption && checkDetailPack && checkDetailPack.memberCount) {
      return Array.from({ length: checkDetailPack.memberCount }).map((_, index) => (
        <div key={index} className="row">
          <div className="form-group col">
            <label>{index === 0 ? "Nhập email người tập" : `Nhập email người tập ${index + 1}`}</label>
            <input className="form-control" type="email" name={`email_${index}`} />
          </div>
        </div>
      ));
    }
    return null;
  };

  const validateField = (name, value) => {
    const newErrors = { ...errors };

    switch (name) {
      case "email":
        const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test(value)) {
          newErrors.email = "Vui lòng nhập địa chỉ email hợp lệ.";
        } else {
          delete newErrors.email; // Clear error if valid
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

  const validateEmail = (email) => {
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!email) {
      setErrors((prev) => ({
        ...prev,
        email: "Email không được để trống",
      }));
      setIsEmailValid(false);
    } else if (!emailPattern.test(email)) {
      setErrors((prev) => ({
        ...prev,
        email: "Email không hợp lệ",
      }));
      setIsEmailValid(false);
    } else {
      setErrors((prev) => ({
        ...prev,
        email: "",
      }));
      setIsEmailValid(true);
    }
  };

  const validateForm = () => {
    const newErrors = {};

    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailPattern.test(formData.email)) {
      newErrors.email = "Vui lòng nhập địa chỉ email hợp lệ.";
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0; // If no errors, form is valid
  };

  const closeModal = () => {
    setCheckDetailPack(null); // Reset thông tin gói
    setSuccessMessage(""); // Xóa thông báo thành công
    BookTrainer.current.style.display = "none";
    document.querySelector(".modal-overlay").style.display = "none";

    // window.close();
  };

  const closeChangeSlotModal = () => {
    setCheckDetailPack(null); // Reset thông tin gói
    setSuccessMessage(""); // Xóa thông báo thành công
    BookTrainer.current.style.display = "none";
    document.querySelector(".modal-overlay").style.display = "none";

    window.close();
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
    // window.location.reload();
    window.close();

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

  const showQr = () => {
    showQrPicture.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  // Hàm để đóng modal thông báo
  const closeQr = () => {
    showQrPicture.current.style.display = "none"; // Ẩn modal
    document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    window.close();
  };

  const openBookTrainerModal = () => {
    setTrainerType(""); // Reset trainer type
    setSelectedOption(""); // Reset selected package option
    setTrainerToAddCourse(null); // Clear selected trainer
    setFormData({
      isTrainerGym: true,
      isTrainerBoxing: true,
      timeSlot: "", // Ensure time slot is reset
      sessionCount: "", // Ensure session count is reset
      trainerRentalPlanId: "", // Ensure trainer plan ID is reset
      boxingMembershipPlanId: "", // Ensure trainer plan ID is reset
    }); // Reset formData to initial state
    setSelectedPackages([]); // Reset the available packages
    setTrainers([]); // Reset the trainers list
    setTimeSlots([]); // Clear the available time slots
    setShowBoxingOptions(false); // Hide boxing options by default
    setShowSessionCount(false); // Hide session count input by default
    setCheckDetailPack(null); // Reset package detail
    setSuccessMessage(""); // Clear success message

    // Show modal and overlay
    BookTrainer.current.style.display = "block";
    BookTrainer.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const handleBookTrainer = async (e) => {
    e.preventDefault();
    setIsFetching(true);

    try {
      const token = localStorage.getItem("token");
      const slotData = {
        userId: courseDetail.userId,
        trainerId: courseDetail.trainerId,
        oldSlotId: courseDetail.currentSlot.timeSlotId,
        newSlotId: selectedTimeslotId,
        scheduleId: courseDetail.scheduleId,
      };

      const response = await fetch("http://localhost:5000/api/Schedule/changeTimeslot", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(slotData),
      });

      const responseText = await response.text();

      // Kiểm tra nếu response không ok
      if (!response.ok) {
        try {
          const errorData = JSON.parse(responseText);
          if (errorData.message) {
            let fullErrorMessage = errorData.message;
            if (errorData.details) {
              fullErrorMessage = `${errorData.message}\n\nChi tiết: ${errorData.details}`;
            }
            showErrorModal(fullErrorMessage);
          } else {
            showErrorModal(responseText);
          }
        } catch (parseError) {
          showErrorModal(responseText);
        }
        return;
      }

      // Nếu response ok thì đóng modal và hiện thông báo thành công
      closeModal();
      showSuccessModal("Đổi slot thành công");
    } catch (error) {
      console.error("Error during change timeslot:", error);
      showErrorModal("Có lỗi xảy ra. Vui lòng thử lại.");
    }finally {
      setIsFetching(false);

    }
  };

  const searchRegistrationByEmail = async () => {
    const token = localStorage.getItem("token");
    setIsFetching(true);
    setHasSearched(true);
    try {
      const dataSend = {
        email: searchEmail,
      };

      const response = await fetch("http://localhost:5000/api/Schedule/getRegistrationsByEmail", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(dataSend),
      });

      const responseText = await response.text();

      // Kiểm tra nếu response không ok
      if (!response.ok) {
        try {
          const errorData = JSON.parse(responseText);
          if (errorData.message) {
            let fullErrorMessage = errorData.message;
            if (errorData.details) {
              fullErrorMessage = `${errorData.message}\n\nChi tiết: ${errorData.details}`;
            }
            showErrorModal(fullErrorMessage);
          } else {
            showErrorModal(responseText);
          }
        } catch (parseError) {
          showErrorModal(responseText);
        }
        return;
      }

      // Nếu response ok thì parse data và set state
      try {
        const responseData = JSON.parse(responseText);
        setCourseData(responseData);
      } catch (parseError) {
        showErrorModal("Lỗi xử lý dữ liệu từ server");
        return;
      }
    } catch (error) {
      console.error("Error searching registration:", error);
      showErrorModal("Có lỗi xảy ra khi tìm kiếm. Vui lòng thử lại.");
    } finally {
      setIsFetching(false);
    }
  };
  const handleSearchEmailChange = (e) => {
    const value = e.target.value;
    setSearchEmail(value);
    validateEmail(value);
    setHasSearched(false); // Reset trạng thái tìm kiếm
    setCourseData([]); // Reset course data
    setSelectedCourseId(""); // Reset selected course
    setCourseDetail({}); // Reset course detail
  };

  const handleChange = async (event) => {
    const selectedCourseIdInput = event.target.value;
    setSelectedCourseId(selectedCourseIdInput);
    // Reset selected timeslot
    setSelectedTimeSlotId("");
    setIsFetching(true);
    if (selectedCourseIdInput) {
      try {
        const response = await axios.post("http://localhost:5000/api/Schedule/getRegistrationDetails", {
          registrationId: selectedCourseIdInput,
        });
        setCourseDetail(response.data);
        console.log("API Response:", response.data);
      } catch (error) {
        console.error("Error posting registration ID:", error);
      } finally {
        setIsFetching(false);
      }
    }
  };
  const handleTrainerChange = (event) => {
    setSelectedTrainerId(event.target.value);
  };

  const handleSelectTimeslotChange = (event) => {
    setSelectedTimeSlotId(event.target.value);
  };
  return (
    <>
      <Header />
      {isFetching && <LoadingSpinner isLoading={true} />}

      <div ref={BookTrainer} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="addTrainerToCourseForm" onSubmit={handleBookTrainer}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">
                  <span style={{ color: "red" }}>Đổi slot</span>
                </h4>
                <a type="button" className="close" onClick={closeChangeSlotModal}>
                  <CloseIcon />
                </a>
              </div>

              <div className="modal-body">
                <div>
                  <label className="me-2">Tìm kiếm email</label>
                  <div className="d-flex flex-column">
                    <div className="d-flex align-items-center mb-2">
                      <input type="email" className={`form-control me-2 ${errors.email ? "is-invalid" : ""}`} value={searchEmail} onChange={handleSearchEmailChange} placeholder="Nhập email" required />
                      <button type="button" className="btn btn-primary" onClick={searchRegistrationByEmail} disabled={!isEmailValid}>
                        Tìm kiếm
                      </button>
                    </div>
                    {errors.email && <div className="invalid-feedback d-block ms-1">{errors.email}</div>}
                  </div>
                </div>
                <div className="row">
                  <div className="form-group col">
                    <label>Gói đã đăng kí</label>
                    {!hasSearched ? (
                      <select className="form-control form-select" disabled>
                        <option value="">Chọn gói</option>
                      </select>
                    ) : courseData.length === 0 ? (
                      <select className="form-control form-select" disabled>
                        <option value="">Không có gói đăng ký</option>
                      </select>
                    ) : (
                      <select className="form-control form-select" name="option" value={selectedCourseId} onChange={handleChange} required>
                        <option value="">Chọn gói</option>
                        {courseData.map((courseData) => (
                          <option key={courseData.registrationId} value={courseData.registrationId}>
                            {courseData.description}
                          </option>
                        ))}
                      </select>
                    )}
                  </div>
                </div>
                <div className="row">
                  <div className="form-group col">
                    <label>Huấn luyện viên</label>
                    <input type="text" className="form-control" name="trainerName" value={courseDetail.trainerName} disabled />
                  </div>
                </div>
                <div className="row">
                  <div className="form-group col">
                    <label>Khung giờ cũ</label>
                    <input type="text" className="form-control" name="timeSlotDisable" value={courseDetail?.currentSlot?.time || ""} disabled />
                  </div>
                </div>
                <div className="row">
                  <div className="form-group col">
                    <label>Chọn khung giờ mới</label>
                    <select
                      className="form-control form-select"
                      name="timeSlot"
                      value={selectedTimeslotId || ""} // Thay đổi ở đây
                      onChange={handleSelectTimeslotChange}
                      required
                    >
                      <option value="">Chọn khung giờ</option> {/* Thay đổi ở đây */}
                      {timeSlots.map((slot) => (
                        <option key={slot.timeSlotId} value={slot.timeSlotId}>
                          {slot.time}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeChangeSlotModal} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
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

      <div className="modal-overlay"></div>

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

      {/* QR Modal */}
      <div ref={showQrPicture} className="modal">
        <div className="modal-dialog modal-dialog-notify">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Thông báo</h4>
              <a type="button" className="close" onClick={closeQr}>
                <CloseIcon />
              </a>
            </div>
            <div className="modal-body">
              {/* Check if qrDataUrl exists and render the QR code */}
              {qrDataUrl ? (
                <div className="qr-code-container">
                  <img src={qrDataUrl} alt="QR Code" className="qr-code-image" />
                </div>
              ) : (
                <p>Không có mã QR để hiển thị.</p>
              )}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-primary" onClick={closeQr}>
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

export default ManageSchedule;
