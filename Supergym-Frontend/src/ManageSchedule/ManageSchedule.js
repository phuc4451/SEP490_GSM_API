import React, { useState, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import CloseIcon from "@mui/icons-material/Close";
import Preloader from "../Preloader/Preloader";

import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageSchedule.css";
import Header from "../Header/Header.js";
import LoadingSpinner from "../utils/LoadingOverlay";

// Hàm lấy button cho lịch của huấn luyện viên
// Hàm lấy button cho lịch của huấn luyện viên

const ManageSchedule = () => {
  const navigate = useNavigate();
  const [allTrainerData, setAllTrainerData] = useState([]); // Store raw data fetched from API
  const [trainers, setTrainers] = useState([]); // Store trainers' data filtered by date
  const [loading, setLoading] = useState(true); // Loading state for the API call
  const [selectedDate, setSelectedDate] = useState(null); // Track selected date
  const timeSlots = ["6:00", "7:00", "8:00", "9:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00"];
  const [isDataLoading, setIsDataLoading] = useState(true);

  // MODAL
  const [currentTrainer, setCurrentTrainer] = useState(null); // for editing trainer
  const viewDetailSlot = useRef(null);
  const ChangeSlotForm = useRef(null);
  const [selectedOption, setSelectedOption] = useState(""); // to store selected option
  const [trainerToAddCourse, setTrainerToAddCourse] = useState(null); // for deletion
  const successModalRef = useRef(null);
  const [errors, setErrors] = useState({});
  const [rentalOptions, setRentalOptions] = useState([]);
  const [successMessage, setSuccessMessage] = useState("");

  const [gymPackages, setGymPackages] = useState([]); // Store Gym packages
  const [boxingPackages, setBoxingPackages] = useState([]); // Store Boxing packages
  const [selectedPackages, setSelectedPackages] = useState([]); // Store the packages based on selected radio
  // CHANGE SLOT FORM
  const [isFetching, setIsFetching] = useState(false);
  const [courseDetail, setCourseDetail] = useState([]);
  const [selectedTimeslotId, setSelectedTimeSlotId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const errorModalRef = useRef(null);
  const [searchEmail, setSearchEmail] = useState("");
  const [isEmailValid, setIsEmailValid] = useState(false);
  const [courseData, setCourseData] = useState([]);
  const [hasSearched, setHasSearched] = useState(false);
  const [selectedCourseId, setSelectedCourseId] = useState("");
  const [checkDetailPack, setCheckDetailPack] = useState(null);
  const [trainerType, setTrainerType] = useState(""); // State cho loại huấn luyện viên
  const [timeChangeSlots, setTimeChangeSlots] = useState([]); // State to store time slots
  const [showBoxingOptions, setShowBoxingOptions] = useState(false); // For showing boxing-specific radio buttons
  const [showSessionCount, setShowSessionCount] = useState(null); // Track if the session count input should be shown
  const showQrPicture = useRef(null);
  const [qrDataUrl, setQrDataUrl] = useState(""); // State to store the QR code data URL

  //BOOK TRAINER FORM
  const BookTrainer = useRef(null);
  const [trainersData, setTrainersData] = useState([]); // Store full trainer data
  const [packagesWithTrainers, setPackagesWithTrainers] = useState([]);
  const [showQR, setShowQR] = useState(true);
  const [moneyToPay, setMoneyToPay] = useState("");
  const [memberCount, setMemberCount] = useState(1);

  // State cho form đăng ký
  const [formTrainers, setFormTrainers] = useState([]);

  const openViewDetailSlot = (trainerName, timeSlot, customers, rentalOption, boxingOption) => {
    const [startTime, endTime] = timeSlot.split("-");

    // If rentalOption is "No rental option", display boxingOption, and vice versa
    let optionToDisplay = "No option";
    if (rentalOption && rentalOption !== "No rental option") {
      optionToDisplay = rentalOption;
    } else if (boxingOption && boxingOption !== "No boxing option") {
      optionToDisplay = boxingOption;
    }

    setFormData({
      ...formData,
      trainerName,
      timeSlot,
      startTime,
      endTime,
      customers: customers.map((customer) => customer.name).join(", "),
      rentalOption, // Add rentalOption to formData
      boxingOption, // Add boxingOption to formData
      optionToDisplay, // Display the correct option
    });

    viewDetailSlot.current.style.display = "block";
    viewDetailSlot.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
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

  const [formData, setFormData] = useState({
    trainerId: "string",
    userId: "string",
    name: "string",
    isTrainerGym: true,
    isTrainerBoxing: true,
    bio: "string",
    specialization: "string",
  });

  const closeModal = () => {
    ChangeSlotForm.current.style.display = "none";
    ChangeSlotForm.current.classList.remove("active");
    viewDetailSlot.current.style.display = "none";
    viewDetailSlot.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentTrainer(null); // Đặt lại trainer hiện tại
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

    // Update the form data
    setFormData({
      ...formData,
      [name]: value,
    });
  };

  const handleRegisterTrainer = () => {
    // Mở tab mới và chuyển đến URL bạn muốn
    // window.open("http://localhost:3000/bookTrainerForm", "_blank");
    openBookTrainerModal();
  };

  const handleChangeSlot = () => {
    // Mở tab mới và chuyển đến URL bạn muốn
    // window.open("http://localhost:3000/changeSlotForm", "_blank");
    openChangeSlotModal();
  };

  // END MODAL

  // SCHEDULE
  const getScheduleButton = (trainerName, timeSlot, selectedDate, trainers) => {
    const trainer = trainers.find((trainer) => trainer.trainerName === trainerName);
    if (trainer && Array.isArray(trainer.slots)) {
      const schedule = trainer.slots.filter((slot) => {
        const startTime = slot.timeSlot.split("-")[0];
        return slot.date === selectedDate && startTime === timeSlot;
      });

      const hasCustomers = schedule.length > 0 && schedule[0].customers.length > 0;
      const customers = hasCustomers ? schedule[0].customers : [];
      const rentalOption = schedule.length > 0 ? schedule[0].rentalOption : "No rental option";
      const boxingOption = schedule.length > 0 ? schedule[0].boxingOption : "No boxing option";

      if (hasCustomers) {
        return (
          <button onClick={() => openViewDetailSlot(trainerName, schedule[0].timeSlot, customers, rentalOption, boxingOption)} className="btn btn-info btn-sm">
            Chi tiết
          </button>
        );
      } else {
        // Display "Trống" when there are no customers
        return <span className="">Trống</span>;
      }
    }
    // Fallback for when there is no trainer data
    return <span className="">Trống</span>;
  };

  //END SCHEDULE
  useEffect(() => {
    // Automatically set today's date when the page is loaded
    const today = new Date().toISOString().split("T")[0]; // Format YYYY-MM-DD
    setSelectedDate(today); // Set the selectedDate to today's date
  }, []);
  // Hàm lấy dữ liệu huấn luyện viên từ API
  useEffect(() => {
    const fetchTrainerData = async (date) => {
      try {
        const token = localStorage.getItem("token");
        if (!token) {
          console.error("No token found in localStorage");
          return;
        }

        const apiUrl = `http://localhost:5000/api/Schedule/Slots/All?inputDate=${date}`;

        const response = await fetch(apiUrl, {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        });

        if (!response.ok) {
          throw new Error("Failed to fetch trainer data");
          return;
        }

        const data = await response.json();
        setAllTrainerData(data);
        setTrainers(data);
      } catch (error) {
        console.error("Error fetching trainer data:", error);
      } finally {
        setIsDataLoading(false);
      }
    };

    // Set today's date và fetch data trong cùng một useEffect
    const today = new Date().toISOString().split("T")[0];
    setSelectedDate(today);
    fetchTrainerData(today);
  }, []); // Chỉ chạy một lần khi component mount

  // useEffect riêng cho việc fetch data khi selectedDate thay đổi
  useEffect(() => {
    // Không fetch data khi lần đầu component mount (selectedDate === null)
    if (!selectedDate) return;

    const fetchTrainerData = async () => {
      setIsDataLoading(true);
      try {
        const token = localStorage.getItem("token");
        if (!token) {
          console.error("No token found in localStorage");
          return;
        }

        const apiUrl = `http://localhost:5000/api/Schedule/Slots/All?inputDate=${selectedDate}`;

        const response = await fetch(apiUrl, {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        });

        if (!response.ok) {
          throw new Error("Failed to fetch trainer data");
          return;
        }

        const data = await response.json();
        setAllTrainerData(data);
        setTrainers(data);
      } catch (error) {
        console.error("Error fetching trainer data:", error);
      } finally {
        setIsDataLoading(false);
      }
    };

    fetchTrainerData();
  }, [selectedDate]); // Chỉ chạy khi selectedDate thay đổi

  // Xử lý thay đổi ngày
  const handleDateChange = (e) => {
    const selected = e.target.value; // Lấy ngày được chọn từ input
    setSelectedDate(selected); // Cập nhật selectedDate và gọi lại fetch API
  };

  //CHANGE SLOT FORM
  const closeChangeSlotModal = () => {
    setCheckDetailPack(null); // Reset thông tin gói
    setSuccessMessage(""); // Xóa thông báo thành công
    ChangeSlotForm.current.style.display = "none";
    document.querySelector(".modal-overlay").style.display = "none";

    window.close();
  };

  const closeErrorModal = () => {
    errorModalRef.current.style.display = "none"; // Ẩn modal
    // document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    // window.location.reload();
  };

  const handleSelectTimeslotChange = (event) => {
    setSelectedTimeSlotId(event.target.value);
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

  const handleSearchEmailChange = (e) => {
    const value = e.target.value;
    setSearchEmail(value);
    validateEmail(value);
    setHasSearched(false); // Reset trạng thái tìm kiếm
    setCourseData([]); // Reset course data
    setSelectedCourseId(""); // Reset selected course
    setCourseDetail({}); // Reset course detail
  };

  const showErrorModal = (message) => {
    setErrorMessage(message); // Cập nhật thông báo
    errorModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const handleChangeSlotForm = async (e) => {
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
    } finally {
      setIsFetching(false);
    }
  };

  const openChangeSlotModal = () => {
    // Reset tất cả state liên quan đến form
    setSearchEmail("");
    setErrors({});
    setIsEmailValid(false);
    setHasSearched(false);
    setCourseData([]);
    setSelectedCourseId("");
    setCourseDetail({});
    setSelectedTimeSlotId("");
    setSuccessMessage("");
    setErrorMessage("");

    // Show modal và overlay
    if (ChangeSlotForm.current) {
      ChangeSlotForm.current.style.display = "block";
      ChangeSlotForm.current.classList.add("active");
      const overlay = document.querySelector(".modal-overlay");
      if (overlay) {
        overlay.style.display = "block";
      }
    }
  };

  const fetchTimeSlots = async () => {
    try {
      const response = await axios.get("http://localhost:5000/api/schedule/timeSlots");
      if (response.data && Array.isArray(response.data)) {
        setTimeChangeSlots(response.data); // Set time slots data
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

  //BOOK TRAINER FORM

  const closeTrainerRegisterModal = () => {
    setCheckDetailPack(null); // Reset thông tin gói
    setSuccessMessage(""); // Xóa thông báo thành công
    BookTrainer.current.style.display = "none";
    document.querySelector(".modal-overlay").style.display = "none";

    // window.close();
  };

  const handlePackageChange = (e) => {
    const selectedPackageId = e.target.value;
    setSelectedOption(selectedPackageId); // Update selected package state

    // Reset trainer-related states
    setTrainerToAddCourse(null); // Reset selected trainer
    setFormTrainers([]); // Clear the trainers list
    setTrainersData([]); // Clear the full trainer data

    // Reset form data related to trainers
    setFormData((prevData) => ({
      ...prevData,
      trainerRentalPlanId: null,
      boxingMembershipPlanId: null,
      timeSlot: "", // Reset time slot
      sessionCount: "", // Reset session count if applicable
    }));

    // Reset checkDetailPack
    setCheckDetailPack(null);

    if (selectedPackageId) {
      // Only fetch trainers if a package is actually selected
      fetchTrainersByPackage(selectedPackageId);
    }
  };

  const fetchTrainersByPackage = async (packageId) => {
    setIsFetching(true);
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
        setFormTrainers(
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
    } finally {
      setIsFetching(false);
    }
  };

  const fetchPackages = async (type) => {
    setIsFetching(true);
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
    } finally {
      setIsFetching(false);
    }
  };

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

  const openBookTrainerModal = () => {
    setTrainerType(""); // Reset trainer type
    setSelectedOption(""); // Reset selected package option
    setTrainerToAddCourse(null); // Clear selected trainer
    setFormData({
      isTrainerGym: true,
      isTrainerBoxing: true,
      qrPayment: true,
      // timeChangeSlots: "", // Ensure time slot is reset
      sessionCount: "", // Ensure session count is reset
      trainerRentalPlanId: "", // Ensure trainer plan ID is reset
      boxingMembershipPlanId: "", // Ensure trainer plan ID is reset
    }); // Reset formData to initial state
    setSelectedPackages([]); // Reset the available packages
    setFormTrainers([]); // Reset the trainers list
    // setTimeChangeSlots([]); // Clear the available time slots
    setShowBoxingOptions(false); // Hide boxing options by default
    setShowSessionCount(false); // Hide session count input by default
    setCheckDetailPack(null); // Reset package detail
    setSuccessMessage(""); // Clear success message

    // Show modal and overlay
    if (BookTrainer.current) {
      BookTrainer.current.style.display = "block";
      BookTrainer.current.classList.add("active");
      const overlay = document.querySelector(".modal-overlay");
      if (overlay) {
        overlay.style.display = "block";
      }
    }
  };

  const handleInputChangeBookTrainer = (e) => {
    const { name, value } = e.target;

    if (name === "qrPayment") {
      setFormData({
        ...formData,
        qrPayment: value === "true", // Chuyển đổi string thành boolean
      });
    } else if (name === "boxingOption") {
      setFormData({
        ...formData,
        isMonWedFri: value === "true",
      });
    } else if (name === "trainerType") {
      setTrainerType(value);
      setSelectedOption("");
      setFormTrainers([]);
      setShowBoxingOptions(value === "Boxing");
      fetchPackages(value);
    } else {
      setFormData({
        ...formData,
        [name]: value,
      });
    }
  };

  const showQr = () => {
    showQrPicture.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  // Hàm để đóng modal thông báo
  const closeQr = () => {
    showQrPicture.current.style.display = "none"; // Ẩn modal
    document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    // window.close();
    window.location.reload();
  };

  const handleBookTrainer = async (e) => {
    e.preventDefault();

    const emailInputs = Array.from(document.querySelectorAll('input[type="email"]'))
      .map((input) => input.value)
      .filter((email) => email.trim() !== "");

    const selectedTimeSlot = formData.timeSlot;
    const trainerRentalPlanId = formData.trainerRentalPlanId;
    const duration = formData.sessionCount;
    const gymDuration = duration ? duration : null;
    const qrPaymentInput = formData.qrPayment;

    const gymData = {
      emails: emailInputs,
      boxingMembershipPlanId: null,
      gymMembershipId: null,
      trainerRentalPlanId: trainerRentalPlanId,
      qrPayment: qrPaymentInput,
      duration: gymDuration,
      selectedTimeSlot: selectedTimeSlot,
      isMonWedFri: trainerType === "TrainerRental" ? true : false,
    };

    const boxingData = {
      emails: emailInputs,
      boxingMembershipPlanId: formData.boxingMembershipPlanId,
      gymMembershipId: null,
      trainerRentalPlanId: null,
      qrPayment: qrPaymentInput,
      duration: 1,
      selectedTimeSlot: formData.timeSlot,
      isMonWedFri: formData.isMonWedFri,
    };

    setIsFetching(true);

    try {
      const token = localStorage.getItem("token");
      if (!token) {
        showErrorModal("Không tìm thấy token xác thực. Vui lòng đăng nhập lại.");
        return;
      }

      let response;
      let responseData = null;

      if (trainerType === "TrainerRental") {
        response = await fetch("http://localhost:5000/api/trainerRentalRegistration", {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify(gymData),
        });
      } else if (trainerType === "Boxing") {
        response = await fetch("http://localhost:5000/api/BoxingRegistration", {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify(boxingData),
        });
      }

      let responsejson = null;
      const contentType = response.headers.get("content-type");
      if (contentType && contentType.includes("application/json")) {
        try {
          responsejson = await response.json();
        } catch (error) {
          console.error("Error parsing JSON:", error);
        }
      }

      if (response.ok) {
        closeModal();


        // Nếu là thanh toán QR và có QR URL
        if (qrPaymentInput && responsejson && responsejson[0] && responsejson[0].qrDataUrl) {
          setQrDataUrl(responsejson[0].qrDataUrl);
          showQr();
        }

        if (!qrPaymentInput && responsejson) {
          const money = responsejson;
          console.log("Money to pay:", money);

          if (money !== undefined && money !== null) {
            const formattedMoney = new Intl.NumberFormat("vi-VN", {
              style: "currency",
              currency: "VND",
            }).format(money);

            showSuccessModal(`Đăng ký thành công! Số tiền cần thanh toán: ${formattedMoney}`);
          } else {
            showSuccessModal("Đăng ký huấn luyện viên thành công");
            console.log("No money value found in response");
          }
        }
      } else {
        const errorText = await response.text();
        if (errorText) {
          try {
            const errorData = JSON.parse(errorText);
            const errorMessage = errorData.message;
            const errorDetails = errorData.details ? `\n${errorData.details}` : '';
            showErrorModal(`${errorMessage}${errorDetails}`);
          } catch {
            showErrorModal(errorText);
          }
        }
        return;
      }
    } catch (error) {
      console.error("Error during trainer registration:", error);
      showErrorModal("Có lỗi xảy ra trong quá trình đăng ký. Vui lòng thử lại.");
    } finally {
      setIsFetching(false);
    }
};

  useEffect(() => {
    if (checkDetailPack && checkDetailPack.memberCount) {
      console.log("Member count changed:", checkDetailPack.memberCount);
      // Bạn có thể thêm logic xử lý sau khi memberCount thay đổi ở đây nếu cần
    }
  }, [checkDetailPack]); // Chạy khi checkDetailPack thay đổi

  // 3. useEffect cho session count handling (cần thêm vào ManageSchedule):
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




  return (
    <>
      <Header />
      {isDataLoading ? <Preloader /> : null}
      {isFetching && <LoadingSpinner isLoading={true} />}

      {/* <!-- ***** Preloader End ***** --> */}
      {/* Schedule Section */}
      <section className="section" id="schedule">
        <div className="container">
          <div className="row">
            <div className="col-lg-6 offset-lg-3">
              <div className="section-heading-home dark-bg">
                <h2>
                  Quản lý <em>Lịch tập</em>
                </h2>
                {/* Date Picker */}
                <section>
                  <div className="container">
                    <div className="row">
                      <div className="col-lg-6 offset-lg-3">
                        <input
                          type="date"
                          onChange={handleDateChange} // Handle date change
                          placeholder="Chọn ngày trong tuần"
                          value={selectedDate}
                        />
                      </div>
                    </div>
                  </div>
                  <button className="btn btn-success btn-sm" onClick={handleRegisterTrainer}>
                    Đăng ký Trainer
                  </button>
                  <button className="btn btn-danger btn-sm ms-3" onClick={handleChangeSlot}>
                    Đổi slot
                  </button>
                </section>
              </div>
            </div>
          </div>
          <div className="row">
            <div className="col">
              <div className="schedule-table filtering">
                <table>
                  <thead>
                    <tr>
                      <th></th> {/* Empty cell for trainer names */}
                      {timeSlots.map((slot, index) => (
                        <th key={index} style={{ textAlign: "center", color: "white" }}>
                          {slot}
                        </th>
                      ))}
                    </tr>
                  </thead>

                  <tbody>
                    {trainers.map((trainer, trainerIndex) => (
                      <tr key={trainerIndex}>
                        <td className="day-time">{trainer.trainerName}</td>
                        {timeSlots.map((slot, timeSlotIndex) => {
                          // Render nút "Chi tiết" hoặc "Thêm" theo thời gian và ngày đã chọn
                          const button = getScheduleButton(trainer.trainerName, slot, selectedDate, trainers);
                          return (
                            <td key={timeSlotIndex}>
                              <div className="schedule-item">{button}</div>
                            </td>
                          );
                        })}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* View Detail Slot */}
      <div ref={viewDetailSlot} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            {/* <form id="addTrainerToCourseForm" onSubmit={handleSubmitAddTrainerToCourse}> */}
            <form id="addTrainerToCourseForm">
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">
                  <span style={{ color: "red" }}>
                    Chi tiết slot của {formData.trainerName} vào lúc {formData.startTime} - {formData.endTime}
                  </span>
                </h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>

              <div className="modal-body">
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Tên khách hàng<span className="icon-input">(*)</span>
                    </label>
                    <textarea className="form-control" name="customers" value={formData.customers || ""} onChange={handleInputChange} required disabled />
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Gói đăng kí</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.optionToDisplay || ""} // Hiển thị gói hợp lệ
                      disabled
                    />
                  </div>
                </div>
              </div>
            </form>
          </div>
        </div>
      </div>

      <div ref={ChangeSlotForm} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="addTrainerToCourseForm" onSubmit={handleChangeSlotForm}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">
                  <span style={{ color: "red" }}>Đổi slot</span>
                </h4>
                <a type="button" className="close" onClick={closeModal}>
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
                      {timeChangeSlots.map((slot) => (
                        <option key={slot.timeSlotId} value={slot.timeSlotId}>
                          {slot.time}
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

      <div ref={BookTrainer} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="addTrainerToCourseForm" onSubmit={handleBookTrainer}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">
                  <span style={{ color: "red" }}>Đăng kí Trainer theo gói</span>
                </h4>
                <a type="button" className="close" onClick={closeTrainerRegisterModal}>
                  <CloseIcon />
                </a>
              </div>

              <div className="modal-body">
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Loại huấn luyện viên<span className="icon-input">(*)</span>
                    </label>
                    <div>
                      <div className="form-check form-check-inline">
                        <input type="radio" id="gym" name="trainerType" value="TrainerRental" checked={trainerType === "TrainerRental"} onChange={handleInputChangeBookTrainer} className="form-check-input" />
                        <label className="form-check-label" htmlFor="gym">
                          Gym
                        </label>
                      </div>
                      <div className="form-check form-check-inline">
                        <input type="radio" id="boxing" name="trainerType" value="Boxing" checked={trainerType === "Boxing"} onChange={handleInputChangeBookTrainer} className="form-check-input" />
                        <label className="form-check-label" htmlFor="boxing">
                          Boxing
                        </label>
                      </div>
                    </div>
                  </div>

                  <div className="form-group col">
                    <label>
                      Chọn kiểu thanh toán <span className="icon-input">(*)</span>
                    </label>
                    <div className="radio-group">
                      <label>
                        <input
                          type="radio"
                          name="qrPayment"
                          value="true"
                          checked={formData.qrPayment === true} // Sử dụng so sánh với boolean
                          onChange={handleInputChangeBookTrainer}
                        />
                        Chuyển khoản
                      </label>
                      <label>
                        <input
                          type="radio"
                          name="qrPayment"
                          value="false"
                          checked={formData.qrPayment === false} // Sử dụng so sánh với boolean
                          onChange={handleInputChangeBookTrainer}
                        />
                        Tiền mặt
                      </label>
                    </div>
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Gói đăng kí</label>
                    <select className="form-control form-select" name="option" value={selectedOption} onChange={handlePackageChange} required>
                      <option value="">Chọn gói</option>
                      {selectedPackages.map((option, index) => (
                        <option key={index} value={option.packageId}>
                          {option.description}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Chọn Huấn luyện viên</label>
                    <select
                      className="form-control"
                      name="trainer"
                      value={trainerToAddCourse ? trainerToAddCourse.trainerId : ""}
                      onChange={(e) => {
                        const selectedTrainer = formTrainers.find((trainer) => trainer.trainerId === e.target.value);
                        setTrainerToAddCourse(selectedTrainer);

                        // Check if selected trainer is a boxing trainer and update based on trainerType
                        if (selectedTrainer && trainerType === "Boxing") {
                          // If it's a boxing trainer, set the trainerId and clear trainerRentalPlanId
                          setFormData((prevData) => ({
                            ...prevData,
                            boxingMembershipPlanId: selectedTrainer ? selectedTrainer.boxingMembershipPlanId : "",
                            trainerRentalPlanId: null, // Clear trainerRentalPlanId for boxing trainers
                          }));
                        } else if (selectedTrainer && trainerType === "TrainerRental") {
                          // If it's a gym trainer, set the trainerRentalPlanId and clear trainerId
                          setFormData((prevData) => ({
                            ...prevData,
                            trainerRentalPlanId: selectedTrainer ? selectedTrainer.trainerRentalPlanId : "",
                            boxingMembershipPlanId: null, // Clear trainerId for gym trainers
                          }));
                        }
                      }}
                      required
                    >
                      <option value="">Chọn Huấn luyện viên</option>
                      {formTrainers.map((trainer, index) => (
                        <option key={index} value={trainer.trainerId}>
                          {trainer.name} - {trainer.specialization}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    {showBoxingOptions && (
                      <>
                        <div className="form-check form-check-inline">
                          <input type="radio" id="option2" name="boxingOption" value="true" onChange={handleInputChange} className="form-check-input" />
                          <label className="form-check-label" htmlFor="option2">
                            Thứ 2, thứ 4, thứ 6
                          </label>
                        </div>
                        <div className="form-check form-check-inline">
                          <input type="radio" id="option3" name="boxingOption" value="false" onChange={handleInputChange} className="form-check-input" />
                          <label className="form-check-label" htmlFor="option3">
                            Thứ 3, thứ 5, thứ 7
                          </label>
                        </div>
                      </>
                    )}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Chọn khung giờ</label>
                    <select
                      className="form-control form-select"
                      name="timeSlot"
                      value={formData.timeSlot || ""} // Thay đổi ở đây
                      onChange={handleInputChange}
                      required
                    >
                      <option value="">Chọn khung giờ</option> {/* Thay đổi ở đây */}
                      {timeChangeSlots.map((slot) => (
                        <option key={slot.timeSlotId} value={slot.timeSlotId}>
                          {slot.time}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    {showSessionCount && (
                      <>
                        <label>Chọn số lượng</label>
                        <input
                          className="form-control"
                          type="number"
                          name="sessionCount" // Đảm bảo rằng input này có name
                          value={formData.sessionCount || ""}
                          onChange={handleInputChange} // Gọi handleInputChange để cập nhật giá trị
                        />
                      </>
                    )}
                  </div>
                </div>

                {renderEmailInputs()}
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeTrainerRegisterModal} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
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
