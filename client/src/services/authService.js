import { apiFetch } from "./api";

// register a new user
export async function register(username, email, password){
    const data = await apiFetch("/auth/register", {
        method: "POST",
        body: JSON.stringify({username, email, password})
    });

    if (data && data.token) {
        storeToken(data);
        storeUser(data);
    }
    return data;
}

// login a user
export async function login(email, password) {
    const data = await apiFetch("/auth/login", {
        method: "POST",
        body: JSON.stringify({ email, password })
    });
    if (data && data.token) {
        storeToken(data);
        storeUser(data);
    }
    return data;
}

// get current user
export async function getCurrentUser() {
    const data = await apiFetch("/users/me", {
        method: "GET",
    });
    return data;
}

// logout
export function logout() {
    localStorage.removeItem("user");
    localStorage.removeItem("jwt_token");
}

// get stored user from local storage
export function getStoredUser() {
    const raw = localStorage.getItem("user");
    if (!raw) return null;
    try {
        return JSON.parse(raw);
    } catch {
        return null;
    }
}

// get stored token from local storage
export function getStoredToken() {
    return localStorage.getItem("jwt_token");
}

// store user data in local storage
export function storeUser(data) {
    if (!data) return;
    localStorage.setItem("user", JSON.stringify(data));
}

// store token in local storage
export function storeToken(data) {
    if (!data) return;
    const token = typeof data === "string" ? data : data.token;
    if (token) {
        localStorage.setItem("jwt_token", token);
    }
}

// confirm email address for password reset
export async function forgotPassword(email) {
    return await apiFetch("/auth/forgot-password", {
        method: "POST",
        body: JSON.stringify({ email })
    });
}

// reset password with confirmation code, email and new password
export async function resetPassword(email, otp, newPassword) {
    return await apiFetch("/auth/reset-password", {
        method: "POST",
        body: JSON.stringify({ email, otp, newPassword })
    });
}

// login or register with Google OAuth
export async function googleLogin(idToken, username = null) {
    const payload = { idToken };
    if (username) {
        payload.username = username;
    }

    const data = await apiFetch("/auth/google", {
        method: "POST",
        body: JSON.stringify(payload)
    });

    if (data && data.token) {
        storeToken(data);
        storeUser(data);
    }
    return data;
}


