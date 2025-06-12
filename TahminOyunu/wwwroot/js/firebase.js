import { initializeApp } from "https://www.gstatic.com/firebasejs/9.22.2/firebase-app.js";
import { getAnalytics } from "https://www.gstatic.com/firebasejs/9.22.2/firebase-analytics.js";

const firebaseConfig = {
    apiKey: "AIzaSyB9cDPrTmFrOVx7UPKgzInOTwIqk1lgcGc",
    authDomain: "guessframe-74018.firebaseapp.com",
    projectId: "guessframe-74018",
    storageBucket: "guessframe-74018.firebasestorage.app",
    messagingSenderId: "227701225744",
    appId: "1:227701225744:web:9ebb8fd5979b53f73b0d95",
    measurementId: "G-989S17KCXP"
};

const app = initializeApp(firebaseConfig);
const analytics = getAnalytics(app);
