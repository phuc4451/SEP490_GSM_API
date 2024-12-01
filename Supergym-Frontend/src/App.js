import Header from './Header/Header';
import Signin from './Signin/Signin';
import Signup from './Signup/Signup';
import Dashboard from './Dashboard/Dashboard';
import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import ManageManager from './ManageManager/ManageManager';
import ManageCustomer from './ManageCustomer/ManageCustomer';
import ManageMembership from './ManageMembership/ManageMembership';
import ManageTrainer from './ManageTrainer/ManageTrainer';
import ManageEquipment from './ManageEquipment/ManageEquipment';
import ManageCourse from './ManageCourse/ManageCourse';
import ManageTrainerCourse from './ManageTrainerCourse/ManageTrainerCourse';
import ManageBoxingCourse from './ManageBoxingCourse/ManageBoxingCourse';
// import ManageSale from './ManageSale/ManageSale';
import ManageFeedback from './ManageFeedback/ManageFeedback';
import ManageSchedule from './ManageSchedule/ManageSchedule';
import BookTrainerForm from './BookTrainerForm/BookTrainerForm';
import Home from './home/home';

const App = () => {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/login" element={<Signin />} />
        <Route path="/signup" element={<Signup />} />
        <Route path="/header" element={<Header />} />
        <Route path="/managManager" element={<ManageManager />} />
        <Route path="/manageCustomer" element={<ManageCustomer />} />
        <Route path="/manageMembership" element={<ManageMembership />} />
        <Route path="/manageTrainer" element={<ManageTrainer />} />
        <Route path="/manageEquipment" element={<ManageEquipment />} />
        <Route path="/manageCourse" element={<ManageCourse />} />
        <Route path="/manageTrainerCourse" element={<ManageTrainerCourse />} />
        <Route path="/manageBoxingCourse" element={<ManageBoxingCourse />} />
        {/* <Route path="/manageSale" element={<ManageSale />} /> */}
        <Route path="/manageFeedback" element={<ManageFeedback />} />
        <Route path="/ManageSchedule" element={<ManageSchedule />} />
        <Route path="/BookTrainerForm" element={<BookTrainerForm />} />
      </Routes>
    </Router>
  );
};

export default App;
