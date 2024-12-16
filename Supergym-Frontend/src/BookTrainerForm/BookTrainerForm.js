import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import CloseIcon from "@mui/icons-material/Close";
import "bootstrap/dist/css/bootstrap.min.css";
import "./BookTrainerForm.css";
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
    qrPayment: true, // Thay đổi giá trị khởi tạo thành boolean true
    timeSlot: "",
    sessionCount: "",
    trainerRentalPlanId: "",
    boxingMembershipPlanId: "",
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
  const [moneyToPay, setMoneyToPay] = useState(""); // State to store the QR code data URL
  const [showQR, setShowQR] = useState(true);

  const [errorMessage, setErrorMessage] = useState("");
  const errorModalRef = useRef(null);
  const [isFetching, setIsFetching] = useState(false);

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
  
    if (name === "qrPayment") {
      setFormData({
        ...formData,
        qrPayment: value === "true" // Chuyển đổi string thành boolean
      });
    } else if (name === "boxingOption") {
      setFormData({
        ...formData,
        isMonWedFri: value === "true",
      });
    } else if (name === "trainerType") {
      setTrainerType(value);
      setSelectedOption("");
      setTrainers([]);
      setShowBoxingOptions(value === "Boxing");
      fetchPackages(value);
    } else {
      setFormData({
        ...formData,
        [name]: value,
      });
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
    } finally {
      setIsFetching(false);
    }
  };

  const handlePackageChange = (e) => {
    const selectedPackageId = e.target.value;
    setSelectedOption(selectedPackageId); // Update selected package state

    // Reset trainer-related states
    setTrainerToAddCourse(null); // Reset selected trainer
    setTrainers([]); // Clear the trainers list
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

  const closeModal = () => {
    setCheckDetailPack(null); // Reset thông tin gói
    setSuccessMessage(""); // Xóa thông báo thành công
    BookTrainer.current.style.display = "none";
    document.querySelector(".modal-overlay").style.display = "none";

    // window.close();
  };

  const closeTrainerRegisterModal = () => {
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
    
    const emailInputs = Array.from(document.querySelectorAll('input[type="email"]'))
      .map((input) => input.value)
      .filter((email) => email.trim() !== "");
  
    const selectedTimeSlot = formData.timeSlot;
    const trainerRentalPlanId = formData.trainerRentalPlanId;
    const duration = formData.sessionCount;
    const gymDuration = duration ? duration : null;
    const qrPaymentInput = formData.qrPayment; // Đã là boolean, không cần chuyển đổi
  
    // Prepare data for Gym
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
  
    // For Boxing data
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
      let responsejson = null;
  
      // Gửi request dựa trên loại trainer
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
  
      // Parse response dựa trên content type
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
            const formattedMoney = new Intl.NumberFormat('vi-VN', {
              style: 'currency',
              currency: 'VND'
            }).format(money);
            
            showSuccessModal(`Đăng ký thành công! Số tiền cần thanh toán: ${formattedMoney}`);
          } else {
            // Fallback message nếu không có moneyToPay
            showSuccessModal("Đăng ký huấn luyện viên thành công");
            console.log("No money value found in response");
          }
        }
      } else {
        // Xử lý lỗi
        const errorMessage = responsejson?.message || "Có lỗi xảy ra khi đăng ký. Vui lòng thử lại.";
        showErrorModal(errorMessage);
      }
    } catch (error) {
      console.error("Error during trainer registration:", error);
      showErrorModal("Có lỗi xảy ra trong quá trình đăng ký. Vui lòng thử lại.");
    } finally {
      setIsFetching(false);
    }
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
                        <input type="radio" id="gym" name="trainerType" value="TrainerRental" checked={trainerType === "TrainerRental"} onChange={handleInputChange} className="form-check-input" />
                        <label className="form-check-label" htmlFor="gym">
                          Gym
                        </label>
                      </div>
                      <div className="form-check form-check-inline">
                        <input type="radio" id="boxing" name="trainerType" value="Boxing" checked={trainerType === "Boxing"} onChange={handleInputChange} className="form-check-input" />
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
                          onChange={handleInputChange}
                        />
                        Chuyển khoản
                      </label>
                      <label>
                        <input
                          type="radio"
                          name="qrPayment"
                          value="false"
                          checked={formData.qrPayment === false} // Sử dụng so sánh với boolean
                          onChange={handleInputChange}
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
                        const selectedTrainer = trainers.find((trainer) => trainer.trainerId === e.target.value);
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
                      {trainers.map((trainer, index) => (
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
                      {timeSlots.map((slot) => (
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
