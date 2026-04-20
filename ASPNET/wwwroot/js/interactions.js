// interactions.js - Using a unique object to avoid global collisions
const interactState = {
    mangaId: null,
    chapterId: null
};

async function initRating(mangaId) {
    interactState.mangaId = mangaId;
    loadRating();
}

async function loadRating() {
    if (!interactState.mangaId) return;
    try {
        const data = await apiFetch(`/Interactions/rating/${interactState.mangaId}`);
        
        const avgEl = document.getElementById("avgRating");
        const countEl = document.getElementById("ratingCount");
        const starsEl = document.getElementById("starsDisplay");

        if (avgEl) avgEl.innerText = data.average.toFixed(1);
        if (countEl) countEl.innerText = `${data.count} đánh giá`;
        
        if (starsEl) {
            let starsHtml = "";
            for (let i = 1; i <= 5; i++) {
                const isHalf = data.average >= i - 0.5 && data.average < i;
                const isFull = data.average >= i;
                
                if (isFull) starsHtml += '<span class="material-symbols-outlined fill-1">star</span>';
                else if (isHalf) starsHtml += '<span class="material-symbols-outlined">star_half</span>';
                else starsHtml += '<span class="material-symbols-outlined">star</span>';
            }
            starsEl.innerHTML = starsHtml;
        }
    } catch (err) {
        console.error("Error loading rating", err);
    }
}

async function submitRating(stars) {
    if (!localStorage.getItem("token")) {
        alert("Vui lòng đăng nhập để đánh giá");
        return;
    }

    try {
        await apiFetch("/Interactions/rate", {
            method: "POST",
            body: JSON.stringify({ mangaId: interactState.mangaId, stars: stars })
        });
        
        alert("Cảm ơn bạn đã đánh giá!");
        loadRating();
    } catch (err) {
        alert("Lỗi: " + err.message);
    }
}

// ==========================================
// COMMENT LOGIC (Used in reader.html)
// ==========================================

async function initComments(chapterId) {
    interactState.chapterId = chapterId;
    console.log("Interactions: Initializing for chapter", chapterId);
    
    const form = document.getElementById("commentForm");
    const prompt = document.getElementById("commentLoginPrompt");
    
    // Improved auth synchronization
    const token = localStorage.getItem("token");
    const userJson = localStorage.getItem("user");
    let hasUser = false;
    if (token && userJson && userJson !== "undefined" && userJson !== "null") {
        hasUser = true;
    }

    if (form && prompt) {
        if (hasUser) {
            form.classList.remove("hidden");
            prompt.classList.add("hidden");
        } else {
            form.classList.add("hidden");
            prompt.classList.remove("hidden");
        }
    }
    
    loadComments();
}

async function loadComments() {
    if (!interactState.chapterId) return;
    const list = document.getElementById("commentsList");
    const countEl = document.getElementById("commentCount");
    if (!list) return;

    try {
        const comments = await apiFetch(`/Interactions/comments/${interactState.chapterId}`);
        
        if (countEl) countEl.innerText = `${comments.length} bình luận`;

        if (comments.length === 0) {
            list.innerHTML = `<p class="text-center text-white/20 text-xs py-8 font-medium">Chưa có bình luận nào. Hãy là người đầu tiên!</p>`;
            return;
        }

        list.innerHTML = comments.map(c => `
            <div class="flex gap-4 group">
                <div class="w-10 h-10 rounded-full bg-white/5 flex items-center justify-center font-black text-xs text-white/20 shrink-0">
                    ${c.username ? c.username.charAt(0).toUpperCase() : '?'}
                </div>
                <div class="flex-1">
                    <div class="flex items-center gap-3 mb-1">
                        <span class="text-xs font-bold text-white uppercase tracking-wider">${c.username}</span>
                        <span class="text-[10px] text-white/20 font-medium">${new Date(c.createdAt).toLocaleDateString()}</span>
                    </div>
                    <p class="text-sm text-white/60 leading-relaxed">${c.content}</p>
                </div>
            </div>
        `).join('');
    } catch (err) {
        console.error("Error loading comments", err);
    }
}

async function submitComment() {
    const input = document.getElementById("commentInput");
    if (!input) return;
    const content = input.value.trim();
    
    if (!content) return;

    try {
        await apiFetch("/Interactions/comment", {
            method: "POST",
            body: JSON.stringify({ chapterId: interactState.chapterId, content: content })
        });
        
        input.value = "";
        loadComments();
    } catch (err) {
        alert("Lỗi gửi bình luận: " + err.message);
    }
}
