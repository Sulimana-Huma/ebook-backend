import React, { useState } from "react";
import { useNavigate } from "react-router-dom";

export default function LoginForm({ onSwitch }) {
  const [formData, setFormData] = useState({ email: "", password: "" });
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const navigate = useNavigate();

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.email || !formData.password) {
      setError("Please fill in both fields.");
      return;
    }

    try {
      const response = await fetch("https://39.61.50.79:7050/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData),
      });

      if (!response.ok) {
        const err = await response.json();
        setError(err.message || "Incorrect email or password");
        return;
      }

      const data = await response.json();
    

      // In LoginForm - update localStorage to match your Vendor model

/*localStorage.setItem(
  "user",
  JSON.stringify({
    customerId: data.user.customerId,   
    vendorId: data.user.vendorId,
    fullName: data.user.fullName,
    email: data.user.email,
    gender: data.user.gender,
    dateOfBirth: data.user.dateOfBirth,
    country: data.user.country,
    userType: data.userType,
  })
);

     if (data.userType === "Vendor") {
  navigate("/vendor-dashboard");
} else if (data.userType === "Customer") {
  navigate("/customer-dashboard");  
} 
else if (data.userType === "Admin") {
  navigate("/admin-dashboard");   
}
else {
  setSuccess(`Welcome ${data.user.fullName}! Logged in as ${data.userType}.`);
}
*/


localStorage.setItem(
  "user",  
  JSON.stringify({
    customerId: data.user.customerId,
    vendorId: data.user.vendorId,
    fullName: data.user.fullName,
    email: data.user.email,
    gender: data.user.gender,
    dateOfBirth: data.user.dateOfBirth,
    country: data.user.country,
    userType: data.userType,
  })
);

if (data.userType === "Vendor") {
  navigate("/vendor-dashboard");
} else if (data.userType === "Customer") {
  navigate("/customer-dashboard");
} else if (data.userType === "Admin") {
  navigate("/admin-dashboard");
} else {
  setSuccess(`Welcome ${data.user.fullName}! Logged in as ${data.userType}.`);
}
    } catch (err) {
      console.error("Login error:", err);
      setError("Something went wrong. Please try again.");
    }
  };

  return (
    <form className="form" onSubmit={handleSubmit}>
      <h2 className="title">Login</h2>
      {error && <div className="message message-error">{error}</div>}
      {success && <div className="message message-success">{success}</div>}

      <div className="field">
        <label className="label">Email</label>
        <input
          className="input"
          type="email"
          name="email"
          value={formData.email}
          onChange={handleChange}
        />
      </div>

      <div className="field">
        <label className="label">Password</label>
        <input
          className="input"
          type="password"
          name="password"
          value={formData.password}
          onChange={handleChange}
        />
      </div>

      <button type="submit" className="fancy-button">Login</button>
      <p className="small-muted">
        Don’t have an account?{" "}
        <span className="link" role="button" onClick={onSwitch}>
          Sign Up
        </span>
      </p>
    </form>
  );
}

/*localStorage.setItem(
  "user",
  JSON.stringify({
    vendorId: data.user.vendorId,
    fullName: data.user.fullName,
    email: data.user.email,
    gender: data.user.gender,
    dateOfBirth: data.user.dateOfBirth,
    country: data.user.country,
    userType: data.userType,
  })
);*/


/*localStorage.setItem(
  "user",
  JSON.stringify({
    vendorId: data.user.vendorId,
    fullName: data.user.fullName,
    email: data.user.email,
    gender: data.user.gender,
    dateOfBirth: data.user.dateOfBirth,
    country: data.user.country,
    userType: data.userType,
  })
);*/