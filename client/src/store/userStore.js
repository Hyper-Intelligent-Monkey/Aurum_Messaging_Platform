import { defineStore, acceptHMRUpdate } from 'pinia';
import { searchUsers, blockUser, unblockUser, getBlockedUsers, updateAvatar, changePassword } from '../services/userService';
import { useAuthStore } from './authStore';
import { storeUser } from '../services/authService';
import { uploadAvatar as uploadAvatarMedia, deleteFile } from '../services/mediaService';

export const useUserStore = defineStore('user', {
    state: () => ({
        searchResults: [],
        blockedUsers: [],
        searchLoading: false,
        blockedLoading: false,
        error: "",
    }),

    actions: {
        clearError() {
            this.error = "";
        },
        // search registered users on platform
        async searchPlatformUsers(query) {
            this.clearError();
            if (!query.trim()) {
                this.searchResults = [];
                return;
            }
            this.searchLoading = true;
            try {
                const results = await searchUsers(query);
                this.searchResults = results;
            } catch (err) {
                this.error = err.message || "Failed to search users.";
                throw err;
            } finally {
                this.searchLoading = false;
            }
        },

        // fetch blocked users
        async fetchBlockedUsers() {
            this.clearError();
            this.blockedLoading = true;
            try {
                const users = await getBlockedUsers();
                this.blockedUsers = users;
            } catch (err) {
                this.error = err.message || "Failed to fetch blocked users.";
                throw err;
            } finally {
                this.blockedLoading = false;
            }
        },

        // block a user
        async blockUserById(userId) {
            this.clearError();
            try {
                await blockUser(userId);
                await this.fetchBlockedUsers();
            } catch (err) {
                this.error = err.message || "Failed to block user.";
                throw err;
            }
        },

        // unblock a user
        async unblockUserById(userId) {
            this.clearError();
            try {
                await unblockUser(userId);
                this.blockedUsers = this.blockedUsers.filter(u => u.id !== userId);
            } catch (err) {
                this.error = err.message || "Failed to unblock user.";
                throw err;
            }
        },

        // update user avatar
        async updateUserAvatar(file) {
            this.clearError();
            try {
                // uploads the avatar image to the server
                const mediaResponse = await uploadAvatarMedia(file);
                // updates the user avatar metadata in the database
                const updatedUser = await updateAvatar(mediaResponse.storedFileName);
                const authStore = useAuthStore();
                authStore.user = updatedUser;
                storeUser(updatedUser);
                return updatedUser;
            } catch (err) {
                this.error = err.message || "Failed to update avatar.";
                throw err;
            }
        },

        // Change password
        async changeUserPassword(currentPassword, newPassword) {
            this.clearError()
            try {
                await changePassword(currentPassword, newPassword);
            } catch (err) {
                this.error = err.message || "Failed to change password.";
                throw err;
            }
        },

        // remove user avatar
        async removeUserAvatar() {
            this.clearError();
            try {
                const authStore = useAuthStore();

                if (authStore.user?.avatar && !authStore.user.avatar.startsWith("http") && !authStore.user.avatar.startsWith("default")) {
                    try {
                        await deleteFile(authStore.user.avatar);
                    } catch (err) {
                        console.warn("Could not delete avatar file from disk, continuing with profile update:", err);
                    }
                }

                const updateUser = await updateAvatar("");

                authStore.user = updateUser;
                storeUser(updateUser);
                return updateUser;
            } catch (error) {
                this.error = error.message || "Failed to remove avatar.";
                throw error;
            }
        },
    },
});

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useUserStore, import.meta.hot));
}