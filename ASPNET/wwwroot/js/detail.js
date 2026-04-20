let currentManga = null;
let selectedChapterId = null;
let selectedChapterPrice = 0;

document.addEventListener("DOMContentLoaded", () => {
    const urlParams = new URLSearchParams(window.location.search);
    const mangaId = urlParams.get('id');

    if (mangaId) {
        loadMangaDetail(mangaId);
        // Khởi kiến hệ thống đánh giá
        if (typeof initRating === "function") initRating(mangaId);
    } else {
        document.getElementById("mangaHeroContent").innerHTML = `<div class="w-full text-center py-20 text-red-500 font-bold">Không tìm thấy Truyện!</div>`;
    }

    // Open Topup Modal handled globally in app.js listener

    document.getElementById("btnConfirmBuy")?.addEventListener("click", buyChapter);
});

async function loadMangaDetail(id) {
    try {
        const token = localStorage.getItem("token");
        const headers = token ? { "Authorization": `Bearer ${token}` } : {};
        
        const response = await fetch(`/api/Mangas/${id}`, { headers });
        if (!response.ok) throw new Error("Lỗi tải thông tin truyện");
        currentManga = await response.json();
        renderDetail(currentManga);
    } catch (error) {
        document.getElementById("mangaHeroContent").innerHTML = `<div class="w-full text-center py-20 text-red-500 font-bold">${error.message}</div>`;
    }
}

function renderDetail(manga) {
    // 1. Update Hero
    const bg = document.getElementById("mangaBg");
    if (manga.coverImageUrl) {
        bg.style.backgroundImage = `url('${manga.coverImageUrl}')`;
    }

    const heroContent = document.getElementById("mangaHeroContent");
    const safeCover = manga.coverImageUrl 
        ? `<img class="w-full h-full object-cover shadow-2xl" src="${manga.coverImageUrl}" alt="${manga.title}">`
        : `<div class="w-full h-full flex flex-col items-center justify-center bg-surface-container-high text-white/20"><span class="material-symbols-outlined text-4xl mb-2">image_not_supported</span></div>`;

    let firstChapterUrl = manga.chapters && manga.chapters.length > 0 
        ? `reader.html?id=${manga.chapters[0].id}`
        : `#`;

    heroContent.innerHTML = `
        <div class="w-full md:w-1/3 max-w-[300px] shrink-0">
            <div class="aspect-[2/3] rounded-2xl overflow-hidden shadow-2xl border border-white/10 mx-auto md:mx-0">
                ${safeCover}
            </div>
        </div>
        <div class="w-full md:w-2/3 flex flex-col justify-end pb-4 text-center md:text-left">
            <span class="inline-block px-3 py-1 bg-white/10 text-white font-bold text-[10px] uppercase tracking-widest rounded mb-4 max-w-max mx-auto md:mx-0 backdrop-blur-sm border border-white/10">
                ${manga.categoryName || "Đang cập nhật"}
            </span>
            <h1 class="text-4xl md:text-6xl font-black font-headline tracking-tighter text-white mb-2 uppercase drop-shadow-lg">${manga.title}</h1>
            <p class="text-xl text-primary font-bold tracking-widest uppercase mb-8">${manga.author || "Khuyết Danh"}</p>
            
            <div class="flex flex-wrap gap-4 justify-center md:justify-start">
                <a href="${firstChapterUrl}" class="${manga.chapters?.length ? 'bg-primary' : 'bg-surface-container-high pointer-events-none opacity-50'} hover:bg-white text-on-primary-container font-black py-4 px-10 rounded uppercase tracking-widest transition-all shadow-xl hover:shadow-primary/30 flex items-center gap-2 text-sm">
                    <span class="material-symbols-outlined">menu_book</span> ${manga.chapters?.length ? 'Đọc Từ Đầu' : 'Chưa có chương'}
                </a>
                <button class="bg-white/5 hover:bg-white/10 border border-white/10 text-white font-bold py-4 px-6 rounded uppercase tracking-widest transition-all flex items-center gap-2 text-sm">
                    <span class="material-symbols-outlined">bookmark_add</span> Theo dõi
                </button>
            </div>
        </div>
    `;

    // 2. Info & Chapters
    document.getElementById("mangaDescription").textContent = manga.description || "Chưa có tóm tắt.";
    document.getElementById("chapterCount").textContent = `${manga.chapters ? manga.chapters.length : 0} Chương`;

    const chapList = document.getElementById("chapterList");
    chapList.innerHTML = "";
    
    if (manga.chapters && manga.chapters.length > 0) {
        manga.chapters.forEach(chap => {
            const isFree = chap.price === 0;
            const isPurchased = chap.isPurchased === true;

            let badgeObj;
            if (isFree) {
                badgeObj = { color: "text-secondary-fixed", bg: "bg-secondary-fixed/10", border:"border-secondary-fixed/20", icon: "check_circle", text: "Miễn Phí" };
            } else if (isPurchased) {
                badgeObj = { color: "text-green-400", bg: "bg-green-400/10", border:"border-green-400/20", icon: "lock_open", text: "Đã Mua" };
            } else {
                badgeObj = { color: "text-primary", bg: "bg-primary/10", border:"border-primary/20", icon: "lock", text: `${chap.price} Xu` };
            }

            const cDiv = document.createElement("div");
            cDiv.className = `flex flex-col md:flex-row justify-between items-start md:items-center p-4 bg-surface-container-low hover:bg-surface-container transition-colors rounded-xl border border-white/5 group cursor-pointer`;
            
            cDiv.innerHTML = `
                <div class="flex items-center gap-4">
                    <div class="w-10 h-10 bg-white/5 rounded-lg flex items-center justify-center font-bold text-white/40 group-hover:text-white transition-colors">
                        #${chap.orderIndex}
                    </div>
                    <div>
                        <h4 class="text-white font-bold tracking-wide">${chap.title}</h4>
                        <p class="text-[10px] text-white/40 uppercase tracking-widest mt-1">${new Date(chap.createdAt).toLocaleDateString('vi-VN')}</p>
                    </div>
                </div>
                <div class="mt-4 md:mt-0 flex items-center gap-3">
                    <span class="px-3 py-1 ${badgeObj.bg} ${badgeObj.border} border text-[10px] font-bold uppercase tracking-widest ${badgeObj.color} rounded flex items-center gap-1">
                        <span class="material-symbols-outlined text-[12px]">${badgeObj.icon}</span> ${badgeObj.text}
                    </span>
                </div>
            `;
            
            cDiv.addEventListener("click", () => handleChapterClick(chap));
            chapList.appendChild(cDiv);
        });
    } else {
        chapList.innerHTML = `<div class="p-8 text-center bg-surface-container-low rounded-xl border border-white/5 text-white/40">Truyện này chưa có chương nào được đăng tải.</div>`;
    }
}

function handleChapterClick(chap) {
    if (chap.price === 0 || chap.isPurchased === true) {
        // Free or Purchased -> go to read
        window.location.href = `reader.html?id=${chap.id}`;
    } else {
        // Paid -> check token & load Paywall
        const token = localStorage.getItem("token");
        if (!token) {
            // Need login
            alert("Vui lòng đăng nhập để đọc chương trả phí!");
            window.location.href = `login.html`;
            return;
        }

        // Hiện Paywall
        selectedChapterId = chap.id;
        selectedChapterPrice = chap.price;
        showPaywall(chap);
    }
}

function showPaywall(chap) {
    const pModal = document.getElementById("paywallModal");
    const pContent = document.getElementById("paywallContent");
    
    // Check balance
    const user = JSON.parse(localStorage.getItem("user") || "{}");
    const balance = user.balance || 0;

    document.getElementById("modalPrice").textContent = `${chap.price} Xu`;
    document.getElementById("modalBalance").textContent = `${balance} Xu`;
    
    const btnBuy = document.getElementById("btnConfirmBuy");
    if (balance < chap.price) {
        document.getElementById("modalBalance").classList.replace("text-secondary-fixed", "text-red-500");
        btnBuy.textContent = "Nạp Thêm Xu";
        btnBuy.onclick = () => {
            closePaywall();
            openTopupModal();
        };
    } else {
        document.getElementById("modalBalance").classList.replace("text-red-500", "text-secondary-fixed");
        btnBuy.textContent = "Mở Khóa";
        btnBuy.onclick = buyChapter;
    }

    document.getElementById("modalError").classList.add("hidden");

    pModal.classList.remove("hidden");
    pModal.classList.add("flex");
    // timeout for animation
    setTimeout(() => {
        pModal.classList.remove("opacity-0");
        pContent.classList.remove("scale-95");
    }, 10);
}

function closePaywall() {
    const pModal = document.getElementById("paywallModal");
    const pContent = document.getElementById("paywallContent");
    
    pModal.classList.add("opacity-0");
    pContent.classList.add("scale-95");
    
    setTimeout(() => {
        pModal.classList.add("hidden");
        pModal.classList.remove("flex");
    }, 300);
}

async function buyChapter() {
    try {
        const btn = document.getElementById("btnConfirmBuy");
        btn.textContent = "Đang xử lý...";
        btn.disabled = true;

        const res = await apiFetch("/Orders", {
            method: "POST",
            body: JSON.stringify({ chapterIds: [selectedChapterId] })
        });

        // if success -> update local storage balance and go to reader
        const user = JSON.parse(localStorage.getItem("user"));
        user.balance -= selectedChapterPrice;
        localStorage.setItem("user", JSON.stringify(user));

        // Update UI
        closePaywall();
        // Call initNavbar directly to update header balance if needed
        if(typeof initNavbar === "function") initNavbar();

        // Redirect to read
        window.location.href = `reader.html?id=${selectedChapterId}`;

    } catch (err) {
        const errP = document.getElementById("modalError");
        errP.textContent = err.message || "Lỗi giao dịch.";
        errP.classList.remove("hidden");
        
        const btn = document.getElementById("btnConfirmBuy");
        btn.textContent = "Thử lại";
        btn.disabled = false;
    }
}
