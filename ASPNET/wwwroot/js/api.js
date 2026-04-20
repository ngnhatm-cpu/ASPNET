const API_BASE = "/api";

async function apiFetch(endpoint, options = {}) {
    const token = localStorage.getItem("token");
    const headers = {
        "Content-Type": "application/json",
        ...options.headers,
    };

    if (token) {
        headers["Authorization"] = `Bearer ${token}`;
    }

    const response = await fetch(`${API_BASE}${endpoint}`, {
        ...options,
        headers,
    });

    if (response.status === 401) {
        // Token hết hạn hoặc không hợp lệ
        localStorage.removeItem("token");
        localStorage.removeItem("user");
        const path = window.location.pathname;
        if (path.includes("/admin/")) {
            window.location.href = "/admin/login.html";
        } else {
            window.location.href = "/login.html";
        }
        return;
    }

    const text = await response.text();
    let data;
    try {
        data = text ? JSON.parse(text) : {};
    } catch (e) {
        data = { message: "Lỗi phản hồi từ server" };
    }

    if (!response.ok) {
        throw new Error(data.message || "Đã có lỗi xảy ra");
    }

    if (response.status === 204) return null;
    return data;
}

function checkAdmin() {
    const user = JSON.parse(localStorage.getItem("user") || "{}");
    if (user.role !== "Admin") {
        window.location.href = "/admin/login.html";
    }
}
