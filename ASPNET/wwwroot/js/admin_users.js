document.addEventListener("DOMContentLoaded", async () => {
    checkAdmin();
    await loadUsers();
});

const userModal = document.getElementById("userModal");
const userForm = document.getElementById("userForm");

async function loadUsers() {
    const tableBody = document.getElementById("userTableBody");
    try {
        const users = await apiFetch("/Users");
        tableBody.innerHTML = users.map(u => `
            <tr class="border-b border-white/5 hover:bg-white/5 transition-all group">
                <td class="px-6 py-4 font-mono text-white/40 text-xs">#USR-${u.id}</td>
                <td class="px-6 py-4 font-bold text-white">${u.username}</td>
                <td class="px-6 py-4 text-white/60">${u.email}</td>
                <td class="px-6 py-4">
                    <span class="px-2 py-1 rounded text-[10px] font-black uppercase ${u.role === 'Admin' ? 'bg-[#ff535b]/20 text-[#ff535b]' : 'bg-white/5 text-white/40'}">
                        ${u.role}
                    </span>
                </td>
                <td class="px-6 py-4 font-black text-[#6fd8cc]">${new Intl.NumberFormat('vi-VN').format(u.balance)} Xu</td>
                <td class="px-6 py-4 text-right space-x-2">
                    <button onclick="editUser(${u.id})" class="text-[#6fd8cc] hover:text-white transition-colors"><span class="material-symbols-outlined">payments</span></button>
                    <button onclick="deleteUser(${u.id})" class="text-[#ff535b] hover:text-white transition-colors"><span class="material-symbols-outlined">delete_forever</span></button>
                </td>
            </tr>
        `).join('');
    } catch (e) { console.error(e); }
}

async function editUser(id) {
    try {
        const user = await apiFetch(`/Users/${id}`);
        document.getElementById("userId").value = user.id;
        document.getElementById("username").value = user.username;
        document.getElementById("email").value = user.email;
        document.getElementById("balance").value = user.balance;
        document.getElementById("role").value = user.role;
        userModal.classList.remove("hidden");
    } catch (e) { alert(e.message); }
}

function closeUserModal() {
    userModal.classList.add("hidden");
}

userForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const id = document.getElementById("userId").value;
    const body = {
        id: parseInt(id),
        username: document.getElementById("username").value,
        email: document.getElementById("email").value,
        balance: parseFloat(document.getElementById("balance").value),
        role: document.getElementById("role").value,
    };

    try {
        await apiFetch(`/Users/${id}`, { method: "PUT", body: JSON.stringify(body) });
        closeUserModal();
        await loadUsers();
    } catch (error) {
        alert("Lỗi: " + error.message);
    }
});

async function deleteUser(id) {
    if (!confirm("Xóa vĩnh viễn người dùng này?")) return;
    try {
        await apiFetch(`/Users/${id}`, { method: "DELETE" });
        await loadUsers();
    } catch (e) { alert(e.message); }
}
