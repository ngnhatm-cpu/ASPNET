document.addEventListener("DOMContentLoaded", async () => {
    checkAdmin();
    await loadCategories();
    await loadMangas();
});

const mangaModal = document.getElementById("mangaModal");
const mangaForm = document.getElementById("mangaForm");

async function loadCategories() {
    try {
        const categories = await apiFetch("/Categories");
        const select = document.getElementById("categoryId");
        select.innerHTML = categories.map(c => `<option value="${c.id}">${c.name}</option>`).join('');
    } catch (e) { console.error(e); }
}

async function loadMangas() {
    const tableBody = document.getElementById("mangaTableBody");
    try {
        const mangas = await apiFetch("/Mangas");
        tableBody.innerHTML = mangas.map(m => {
            const defaultImg = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI2MCIgaGVpZ2h0PSI4MCI+PHJlY3Qgd2lkdGg9IjEwMCUiIGhlaWdodD0iMTAwJSIgZmlsbD0iIzFmMWYyNSIvPjx0ZXh0IHg9IjUwJSIgeT0iNTAlIiBmb250LWZhbWlseT0ic2Fucy1zZXJpZiIgZm9udC1zaXplPSIxMCIgZmlsbD0iIzU1NSIgZHk9Ii4zZW0iIHRleHQtYW5jaG9yPSJtaWRkbGUiPk5PIElNRzwvdGV4dD48L3N2Zz4=";
            const imgSrc = m.coverImageUrl ? m.coverImageUrl : defaultImg;
            return `
            <tr class="border-b border-white/5 hover:bg-white/5 transition-all group">
                <td class="px-6 py-4">
                    <img src="${imgSrc}" onerror="this.src='${defaultImg}'" class="w-12 h-16 object-cover rounded shadow-lg">
                </td>
                <td class="px-6 py-4 font-bold text-white">${m.title}</td>
                <td class="px-6 py-4 text-[#ff535b] text-xs font-bold uppercase tracking-widest">${m.categoryName || 'N/A'}</td>
                <td class="px-6 py-4 text-white/60">${m.author}</td>
                <td class="px-6 py-4 text-right space-x-2">
                    <button onclick="editManga(${m.id})" class="text-[#6fd8cc] hover:text-white transition-colors"><span class="material-symbols-outlined">edit</span></button>
                    <button onclick="manageChapters(${m.id})" class="text-secondary hover:text-white transition-colors"><span class="material-symbols-outlined">list</span></button>
                    <button onclick="deleteManga(${m.id})" class="text-[#ff535b] hover:text-white transition-colors"><span class="material-symbols-outlined">delete</span></button>
                </td>
            </tr>
        `}).join('');
    } catch (e) { console.error(e); }
}

function openMangaModal(manga = null) {
    mangaModal.classList.remove("hidden");
    document.getElementById("coverImageFile").value = "";
    const preview = document.getElementById("coverPreview");

    if (manga) {
        document.getElementById("modalTitle").textContent = "Cập Nhật Truyện";
        document.getElementById("mangaId").value = manga.id;
        document.getElementById("title").value = manga.title;
        document.getElementById("author").value = manga.author;
        document.getElementById("categoryId").value = manga.categoryId;
        document.getElementById("coverImageUrl").value = manga.coverImageUrl;
        document.getElementById("description").value = manga.description;
        
        if (manga.coverImageUrl) {
            preview.src = manga.coverImageUrl;
            preview.classList.remove("hidden");
        } else {
            preview.classList.add("hidden");
        }
    } else {
        document.getElementById("modalTitle").textContent = "Thêm Truyện Mới";
        mangaForm.reset();
        document.getElementById("mangaId").value = "";
        document.getElementById("coverImageUrl").value = "";
        preview.classList.add("hidden");
    }
}

function closeMangaModal() {
    mangaModal.classList.add("hidden");
}

document.getElementById("coverImageFile").addEventListener("change", async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append("file", file);

    try {
        const response = await fetch("/api/Upload/image", {
            method: "POST",
            headers: {
                "Authorization": `Bearer ${localStorage.getItem("token")}`
            },
            body: formData
        });

        if (!response.ok) throw new Error("Lỗi tải ảnh");
        const data = await response.json();
        
        document.getElementById("coverPreview").src = data.url;
        document.getElementById("coverPreview").classList.remove("hidden");
        document.getElementById("coverImageUrl").value = data.url; // Cập nhật input ẩn để lưu vào DB
    } catch (err) {
        alert(err.message);
    }
});

mangaForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const id = document.getElementById("mangaId").value;
    const body = {
        title: document.getElementById("title").value,
        author: document.getElementById("author").value,
        price: 0,
        categoryId: parseInt(document.getElementById("categoryId").value),
        coverImageUrl: document.getElementById("coverImageUrl").value,
        description: document.getElementById("description").value,
    };

    try {
        if (id) {
            body.id = parseInt(id);
            await apiFetch(`/Mangas/${id}`, { method: "PUT", body: JSON.stringify(body) });
            alert("Cập nhật truyện thành công!");
        } else {
            await apiFetch("/Mangas", { method: "POST", body: JSON.stringify(body) });
            alert("Thêm truyện mới thành công!");
        }
        closeMangaModal();
        await loadMangas();
    } catch (error) {
        alert("Lỗi: " + error.message);
    }
});

async function editManga(id) {
    try {
        const manga = await apiFetch(`/Mangas/${id}`);
        openMangaModal(manga);
    } catch (e) { alert(e.message); }
}

async function deleteManga(id) {
    if (!confirm("Bạn có chắc chắn muốn xóa bộ truyện này? Tất cả chương truyện liên quan cũng sẽ bị xóa.")) return;
    try {
        await apiFetch(`/Mangas/${id}`, { method: "DELETE" });
        await loadMangas();
    } catch (e) { alert(e.message); }
}

function manageChapters(id) {
    // Chuyển hướng sang trang quản lý chương với mangaId
    window.location.href = `chapters.html?mangaId=${id}`;
}
