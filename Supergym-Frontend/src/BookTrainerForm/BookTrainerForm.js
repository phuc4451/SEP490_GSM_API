import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import CloseIcon from "@mui/icons-material/Close";
import "bootstrap/dist/css/bootstrap.min.css";
import Header from "../Header/Header.js";

const ManageSchedule = () => {
  const [trainerType, setTrainerType] = useState(""); // State cho loại huấn luyện viên
  const [selectedOption, setSelectedOption] = useState(""); // State cho gói đã chọn
  const BookTrainer = useRef(null);
  const [trainerToAddCourse, setTrainerToAddCourse] = useState(null); // for deletion
  const [successMessage, setSuccessMessage] = useState("");
  const successModalRef = useRef(null);
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
    setFormData({
      ...formData,
      [name]: value,
    });

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

  // useEffect để theo dõi sự thay đổi của checkIsMonthly
  useEffect(() => {
    if (checkDetailPack && checkDetailPack.isMonthlyPackage !== undefined) {
      // Kiểm tra nếu `checkDetailPack` đã được cập nhật và có giá trị `isMonthlyPackage`
      if (checkDetailPack.isMonthlyPackage) {
        setShowSessionCount(false); // Ẩn trường nhập liệu nếu là gói Monthly
      } else {
        setShowSessionCount(true); // Hiển thị trường nhập liệu nếu không phải gói Monthly
      }
    }
  }, [checkDetailPack]);

  useEffect(() => {
    if (checkDetailPack && checkDetailPack.memberCount) {
      console.log("Member count changed:", checkDetailPack.memberCount);
      // Bạn có thể thêm logic xử lý sau khi memberCount thay đổi ở đây nếu cần
    }
  }, [checkDetailPack]); // Chạy khi checkDetailPack thay đổi

  const renderEmailInputs = () => {
    if (checkDetailPack && checkDetailPack.memberCount) {
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
    // Reset tất cả các trạng thái về giá trị mặc định
    setTrainerType(""); // Reset loại huấn luyện viên
    setSelectedOption(""); // Reset gói đã chọn
    setTrainers([]); // Xóa danh sách huấn luyện viên
    setTrainerToAddCourse(null); // Reset huấn luyện viên đã chọn
    setShowSessionCount(null); // Reset hiển thị số buổi
    setMemberCount(1); // Reset số lượng người tập về 1
    setFormData({
      isTrainerGym: true,
      isTrainerBoxing: true,
    }); // Reset formData nếu cần
    setCheckDetailPack(null); // Reset thông tin gói
    setSuccessMessage(""); // Xóa thông báo thành công
    BookTrainer.current.style.display = "none";
    document.querySelector(".modal-overlay").style.display = "none";

    // window.close();
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

  const openBookTrainerModal = () => {
    setSelectedOption(""); // Reset lựa chọn khi mở modal
    BookTrainer.current.style.display = "block";
    BookTrainer.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const handleBookTrainer = async (e) => {
    e.preventDefault();
  
    // Collect form data based on trainer type (Gym or Boxing)
    const emailInputs = Array.from(document.querySelectorAll('input[type="email"]')).map((input) => input.value);
    const selectedTimeSlot = formData.timeSlot; // Assuming timeSlot is captured in formData
    const trainerRentalPlanId = formData.trainerRentalPlanId;  // Make sure this value is set
    const duration = formData.sessionCount; // sessionCount for Gym
    const qrPayment = formData.qrPayment;
    let gymMembershipId = formData.gymMembershipId;
    let boxingMembershipPlanId = formData.boxingMembershipPlanId;
    let trainerId = formData.trainerId; // The selected trainer's ID
  
    // Prepare data to send in the request body
    const data = {
      emails: emailInputs, // Emails from input fields
      boxingMembershipPlanId: trainerType === "Boxing" ? boxingMembershipPlanId : null,
      gymMembershipId: trainerType === "Gym" ? gymMembershipId : null,
      trainerRentalPlanId: trainerRentalPlanId,  // Ensure this is included
      qrPayment: qrPayment,
      duration: duration,
      selectedTimeSlot: selectedTimeSlot,
      isMonWedFri: true, // Assuming this is a fixed option for now
    };
  
    try {
        
      // Get token from local storage
      const token = localStorage.getItem("token");
  
      // Send the POST request
      var response = await axios.post("http://localhost:5000/api/trainerRentalRegistration", data, {
        headers: { Authorization: `Bearer ${token}` },
      });
  
      // Extract the response body directly as an object
      const responseData = response.data; // This should be an object, not an array
  
      // Check if the response contains the necessary properties (qrDataUrl and trainerRentalPlan)
      if (responseData && responseData.qrDataUrl && responseData.details && responseData.details.trainerRentalPlan) {
        const qrDataUrl = responseData.qrDataUrl;
        const trainerRentalPlan = responseData.details.trainerRentalPlan;
  
        // Log the QR data URL and trainer rental plan details for debugging
        console.log("QR Data URL:", qrDataUrl);
        console.log("Trainer Rental Plan Details:", trainerRentalPlan);
  
        // Show success message
        showSuccessModal("Đăng ký huấn luyện viên thành công!");
  
        // Optionally, display QR code or rental plan details (you can use qrDataUrl in an <img> element or in a modal)
  
        closeModal(); // Close the modal after success
      } else {
        // Handle case where response doesn't contain expected structure
        showSuccessModal("Có lỗi khi đăng ký huấn luyện viên. Dữ liệu trả về không hợp lệ.");
      }
    } catch (error) {
      // Handle errors from the API call
      console.error("Error during trainer registration:", error);
      showSuccessModal("Có lỗi khi đăng ký huấn luyện viên. Vui lòng thử lại.");
    }
  };
  
  
  
  

  return (
    <>
      <Header />
      <section>
        <button className="btn btn-success btn-sm" onClick={openBookTrainerModal}>
          Đăng kí Trainer
        </button>
      </section>

      <div ref={BookTrainer} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="addTrainerToCourseForm" onSubmit={handleBookTrainer}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">
                  <span style={{ color: "red" }}>Đăng kí Trainer theo gói</span>
                </h4>
                <a type="button" className="close" onClick={closeModal}>
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
    const selectedTrainer = trainers.find(
      (trainer) => trainer.trainerId === e.target.value
    );
    setTrainerToAddCourse(selectedTrainer);
    // Update formData to include the trainerRentalPlanId
    setFormData((prevData) => ({
      ...prevData,
      trainerRentalPlanId: selectedTrainer ? selectedTrainer.trainerRentalPlanId : ""
    }));
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
                          <input type="radio" id="option2" name="boxingOption" value="2" onChange={handleInputChange} className="form-check-input" />
                          <label className="form-check-label" htmlFor="option2">
                            Thứ 2, thứ 4, thứ 6
                          </label>
                        </div>
                        <div className="form-check form-check-inline">
                          <input type="radio" id="option3" name="boxingOption" value="3" onChange={handleInputChange} className="form-check-input" />
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
                    <select className="form-control form-select" name="timeSlot" onChange={handleInputChange} required>
                      <option value="">Chọn khung giờ</option>
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
                        <label>Chọn số buổi tập</label>
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
    </>
  );
};

export default ManageSchedule;
