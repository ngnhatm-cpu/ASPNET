const urlParams = new URLSearchParams(window.location.search);
const mangaId = parseInt(urlParams.get('mangaId'));

if (!mangaId) {
    window.location.href = "mangas.html";
}

document.addEventListener("DOMContentLoaded", async () => {
    checkAdmin();
    await loadMangaInfo();
    await loadChapters();
});

const chapterModal = document.getElementById("chapterModal");
const chapterForm = document.getElementById("chapterForm");

async function loadMangaInfo() {
    try {
        const manga = await apiFetch(`/Mangas/${mangaId}`);
        document.getElementById("mangaTitleHeader").textContent = manga.title;
        document.title = `Quản lý: ${manga.title} | Admin`;
    } catch (e) { console.error(e); }
}

async function loadChapters() {
    const tableBody = document.getElementById("chapterTableBody");
    try {
        const chapters = await apiFetch(`/mangas/${mangaId}/chapters`);
        if (!chapters || chapters.length === 0) {
            tableBody.innerHTML = `<tr><td colspan="5" class="text-center py-8 text-white/40">Chưa có chương nào</td></tr>`;
            return;
        }
        tableBody.innerHTML = chapters.map(c => `
            <tr class="border-b border-white/5 hover:bg-white/5 transition-all group">
                <td class="px-6 py-4 font-mono text-white/40 text-xs">${c.orderIndex}</td>
                <td class="px-6 py-4 font-bold text-white">${c.title}</td>
                <td class="px-6 py-4 font-black">${c.price == 0 ? '<span class="text-green-400">Miễn phí</span>' : new Intl.NumberFormat('vi-VN').format(c.price) + ' Xu'}</td>
                <td class="px-6 py-4 text-xs italic text-white/40 max-w-[200px] truncate" title="${c.filePath || ''}">${c.filePath ? '✅ Có ảnh' : '❌ Chưa có ảnh'}</td>
                <td class="px-6 py-4 text-right space-x-2">
                    <button onclick="editChapter(${c.id})" class="text-[#6fd8cc] hover:text-white transition-colors"><span class="material-symbols-outlined">edit</span></button>
                    <button onclick="deleteChapter(${c.id})" class="text-[#ff535b] hover:text-white transition-colors"><span class="material-symbols-outlined">delete</span></button>
                </td>
            </tr>
        `).join('');
    } catch (e) { console.error(e); tableBody.innerHTML = `<tr><td colspan="5" class="text-center py-8 text-red-500">Lỗi tải dữ liệu</td></tr>`;}
}

function openChapterModal(chapter = null) {
    chapterModal.classList.remove("hidden");
    const fileInput = document.getElementById("chapterImages");
    if (fileInput) fileInput.value = "";
    document.getElementById("uploadStatus").textContent = "";
    document.getElementById("uploadStatus").className = "text-xs mt-1";

    if (chapter) {
        document.getElementById("chapterModalTitle").textContent = "Cập Nhật Chương";
        document.getElementById("chapterId").value = chapter.id;
        document.getElementById("chapterTitle").value = chapter.title;
        document.getElementById("chapterPrice").value = chapter.price;
        document.getElementById("orderIndex").value = chapter.orderIndex;
        document.getElementById("filePath").value = chapter.filePath || "";
    } else {
        document.getElementById("chapterModalTitle").textContent = "Thêm Chương Mới";
        chapterForm.reset();
        document.getElementById("chapterId").value = "";
        document.getElementById("filePath").value = "";
    }
}

function closeChapterModal() {
    chapterModal.classList.add("hidden");
}

// File selection preview
document.getElementById("chapterImages")?.addEventListener("change", (e) => {
    const statusEl = document.getElementById("uploadStatus");
    const count = e.target.files.length;
    if (count > 0) {
        statusEl.textContent = `✅ Đã chọn ${count} ảnh (${Array.from(e.target.files).map(f => f.name).join(', ').substring(0, 60)}...)`;
        statusEl.className = "text-xs mt-1 text-green-400";
    } else {
        statusEl.textContent = "";
    }
});

chapterForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const submitBtn = chapterForm.querySelector("button[type='submit']");
    submitBtn.disabled = true;
    submitBtn.textContent = "Đang xử lý...";

    const id = document.getElementById("chapterId").value;
    const fileInput = document.getElementById("chapterImages");
    const hasFiles = fileInput && fileInput.files.length > 0;
    const statusEl = document.getElementById("uploadStatus");

    const body = {
        mangaId: mangaId,
        title: document.getElementById("chapterTitle").value,
        price: parseFloat(document.getElementById("chapterPrice").value) || 0,
        orderIndex: parseInt(document.getElementById("orderIndex").value) || 1,
        filePath: document.getElementById("filePath").value || null,
    };

    try {
        let savedChapterId;

        if (id) {
            // Cập nhật chapter trước
            body.id = parseInt(id);
            savedChapterId = parseInt(id);
            await apiFetch(`/Chapters/${id}`, { method: "PUT", body: JSON.stringify(body) });
        } else {
            // Tạo chapter mới trước để lấy ID
            const newChapter = await apiFetch("/Chapters", { method: "POST", body: JSON.stringify(body) });
            savedChapterId = newChapter.id;
        }

        // Upload ảnh sau khi có chapterId
        if (hasFiles && savedChapterId) {
            statusEl.textContent = `⏳ Đang tải ${fileInput.files.length} ảnh lên...`;
            statusEl.className = "text-xs mt-1 text-yellow-400";

            const formData = new FormData();
            for (let i = 0; i < fileInput.files.length; i++) {
                formData.append("files", fileInput.files[i]);
            }

            const uploadRes = await fetch(`/api/Upload/chapter-pages/${savedChapterId}`, {
                method: "POST",
                headers: { "Authorization": `Bearer ${localStorage.getItem('token')}` },
                body: formData
            });

            if (!uploadRes.ok) {
                const errText = await uploadRes.text();
                statusEl.textContent = `❌ Chương đã lưu nhưng ảnh thất bại: ${errText}`;
                statusEl.className = "text-xs mt-1 text-red-400";
                submitBtn.disabled = false;
                submitBtn.textContent = "Lưu Chương";
                return;
            }

            const uploadData = await uploadRes.json();
            statusEl.textContent = `✅ Tải lên ${uploadData.count} ảnh thành công!`;
            statusEl.className = "text-xs mt-1 text-green-400";

            // Lưu JSON array của Cloudinary URLs vào filePath
            const finalBody = { ...body, id: savedChapterId, filePath: JSON.stringify(uploadData.urls) };
            await apiFetch(`/Chapters/${savedChapterId}`, { method: "PUT", body: JSON.stringify(finalBody) });
        }

        // Thành công
        setTimeout(async () => {
            alert("✅ Đã lưu chương và tải ảnh thành công!");
            closeChapterModal();
            await loadChapters();
        }, hasFiles ? 800 : 0);

    } catch (error) {
        alert("Lỗi: " + error.message);
    } finally {
        submitBtn.disabled = false;
        submitBtn.textContent = "Lưu Chương";
    }
});

async function editChapter(id) {
    try {
        const chapter = await apiFetch(`/Chapters/${id}`);
        openChapterModal(chapter);
    } catch (e) { alert(e.message); }
}

async function deleteChapter(id) {
    if (!confirm("Xóa chương truyện này?")) return;
    try {
        await apiFetch(`/Chapters/${id}`, { method: "DELETE" });
        await loadChapters();
    } catch (e) { alert(e.message); }
}
