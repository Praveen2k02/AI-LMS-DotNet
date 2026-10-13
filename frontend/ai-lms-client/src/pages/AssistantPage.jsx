import {
    useCallback,
    useEffect,
    useRef,
    useState
} from "react";

import {
    ArrowUp,
    Bot,
    MessageCircle,
    Plus,
    RotateCcw,
    Sparkles,
    Trash2,
    UserRound
} from "lucide-react";

import { api, errorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";


/*
============================================================
ROLE-BASED AI CONFIGURATION
============================================================
*/

const assistantConfig = {
    Student: {
        title: "AI Study Assistant",
        subtitle: "Friendly, beginner-first learning support",

        welcome:
            "Hello! I'm your AI Study Assistant. Ask me a course question, " +
            "request an example, ask me to explain a difficult concept, " +
            "or tell me what you're practising today.",

        prompts: [
            "Explain dependency injection simply",
            "What is an INNER JOIN?",
            "Help me plan a study session",
            "Give me a React hooks example"
        ],

        meta: "AI study assistant",

        notification: "AI tutor replied."
    },

    Trainer: {
        title: "AI Teaching Assistant",
        subtitle: "Teaching, course and assessment support",

        welcome:
            "Hello! I'm your AI Teaching Assistant. I can help you create " +
            "lesson ideas, assignments, quiz questions, examples, summaries " +
            "and teaching material.",

        prompts: [
            "Create a lesson plan for dependency injection",
            "Give me 5 quiz questions about SQL JOINs",
            "Suggest an assignment for ASP.NET Core",
            "How can I explain APIs to beginners?"
        ],

        meta: "AI teaching assistant",

        notification: "AI teaching assistant replied."
    },

    Admin: {
        title: "AI LMS Assistant",
        subtitle: "LMS management and learning insights",

        welcome:
            "Hello! I'm your AI LMS Assistant. I can help with course " +
            "management ideas, trainer support, student support, reports, " +
            "learning insights and LMS improvements.",

        prompts: [
            "How can I improve student engagement?",
            "Suggest useful LMS performance metrics",
            "How can trainers support weak students?",
            "Suggest improvements for our courses"
        ],

        meta: "AI LMS assistant",

        notification: "AI LMS assistant replied."
    }
};


const defaultConfig = assistantConfig.Student;


/*
============================================================
COMPONENT
============================================================
*/

export default function AssistantPage() {

    const { user } = useAuth();

    const notify = useToast();

    /*
    ----------------------------------------------------------
    CURRENT USER ROLE
    ----------------------------------------------------------
    */

    const role =
        user?.role === "Admin" ||
            user?.role === "Trainer" ||
            user?.role === "Student"
            ? user.role
            : "Student";


    const config =
        assistantConfig[role] || defaultConfig;


    /*
    ----------------------------------------------------------
    STATE
    ----------------------------------------------------------
    */

    const [conversations, setConversations] = useState([]);

    const [activeId, setActiveId] = useState(null);

    const [messages, setMessages] = useState([
        {
            role: "assistant",
            content: config.welcome
        }
    ]);

    const [input, setInput] = useState("");

    const [loading, setLoading] = useState(true);

    const [sending, setSending] = useState(false);

    const bottomRef = useRef(null);

    const inputRef = useRef(null);


    /*
    ============================================================
    LOAD CONVERSATIONS
    ============================================================
    */

    const loadList = useCallback(async () => {

        try {

            const { data } =
                await api.get("/ai/conversations");

            setConversations(data);

            return data;

        } catch (err) {

            notify(
                errorMessage(err),
                "error"
            );

            return [];

        } finally {

            setLoading(false);

        }

    }, [notify]);


    /*
    ============================================================
    INITIAL LOAD
    ============================================================
    */

    useEffect(() => {

        loadList();

    }, [loadList]);


    /*
    ============================================================
    RESET WELCOME MESSAGE WHEN ROLE CHANGES
    ============================================================
    */

    useEffect(() => {

        setActiveId(null);

        setMessages([
            {
                role: "assistant",
                content: config.welcome
            }
        ]);

    }, [role, config.welcome]);


    /*
    ============================================================
    AUTO SCROLL
    ============================================================
    */

    useEffect(() => {

        bottomRef.current?.scrollIntoView({
            behavior: "smooth"
        });

    }, [messages, sending]);


    /*
    ============================================================
    OPEN CONVERSATION
    ============================================================
    */

    async function openConversation(id) {

        try {

            const { data } =
                await api.get(
                    `/ai/conversations/${id}`
                );

            setActiveId(data.id);

            setMessages(
                data.messages.length
                    ? data.messages
                    : [
                        {
                            role: "assistant",
                            content: config.welcome
                        }
                    ]
            );

        } catch (err) {

            notify(
                errorMessage(err),
                "error"
            );

        }

    }


    /*
    ============================================================
    NEW CONVERSATION
    ============================================================
    */

    function newConversation() {

        setActiveId(null);

        setMessages([
            {
                role: "assistant",
                content: config.welcome
            }
        ]);

        setInput("");

        inputRef.current?.focus();

    }


    /*
    ============================================================
    SEND MESSAGE
    ============================================================
    */

    async function send(message = input) {

        const text =
            message.trim();

        if (!text || sending)
            return;


        const priorId =
            activeId;


        /*
        Add user message immediately
        */

        setMessages((all) => [

            ...all,

            {
                role: "user",
                content: text
            }

        ]);


        setInput("");

        setSending(true);


        try {

            const { data } =
                await api.post(
                    "/ai/chat",
                    {
                        message: text,
                        conversationId: priorId
                    }
                );


            setActiveId(
                data.conversationId
            );


            setMessages((all) => [

                ...all,

                {
                    role: "assistant",
                    content: data.answer
                }

            ]);


            notify(

                data.usedOpenAi

                    ? config.notification

                    : `${config.title} reply ready · configure OPENAI_API_KEY for live AI.`,

                data.usedOpenAi
                    ? "success"
                    : "info"

            );


            await loadList();

        } catch (err) {

            setMessages((all) => [

                ...all,

                {
                    role: "assistant",
                    content:
                        `I couldn't send that message. ${errorMessage(err)}`
                }

            ]);


            notify(
                errorMessage(err),
                "error"
            );

        } finally {

            setSending(false);

        }

    }


    /*
    ============================================================
    DELETE CONVERSATION
    ============================================================
    */

    async function remove(id) {

        if (
            !window.confirm(
                "Delete this saved conversation?"
            )
        ) {
            return;
        }


        try {

            await api.delete(
                `/ai/conversations/${id}`
            );


            if (activeId === id) {

                newConversation();

            }


            notify(
                "Conversation deleted.",
                "info"
            );


            await loadList();

        } catch (err) {

            notify(
                errorMessage(err),
                "error"
            );

        }

    }


    /*
    ============================================================
    RENDER
    ============================================================
    */

    return (

        <div className="assistant-layout">

            {/* =====================================================
          CONVERSATION HISTORY
      ===================================================== */}

            <aside className="assistant-history panel">

                <div className="assistant-history-head">

                    <div>

                        <span className="eyebrow">
                            YOUR SPACE
                        </span>

                        <h3>
                            Conversations
                        </h3>

                    </div>


                    <button
                        className="icon-btn assistant-new-icon"
                        onClick={newConversation}
                        title="New chat"
                    >

                        <Plus size={18} />

                    </button>

                </div>


                <button
                    className="btn btn-primary btn-block assistant-new-btn"
                    onClick={newConversation}
                >

                    <Plus size={16} />

                    New conversation

                </button>


                <div className="conversation-list">

                    {loading ? (

                        <div className="history-loading">

                            <span className="spinner spinner-dark" />

                            Loading history…

                        </div>

                    ) : (

                        conversations.map((c) => (

                            <div
                                className={
                                    `conversation-item ${activeId === c.id
                                        ? "conversation-active"
                                        : ""
                                    }`
                                }
                                key={c.id}
                            >

                                <button
                                    onClick={() =>
                                        openConversation(c.id)
                                    }
                                >

                                    <MessageCircle size={15} />

                                    <span>
                                        {c.title}
                                    </span>

                                </button>


                                <button
                                    className="conversation-delete"
                                    onClick={() =>
                                        remove(c.id)
                                    }
                                    title="Delete conversation"
                                >

                                    <Trash2 size={13} />

                                </button>

                            </div>

                        ))

                    )}


                    {!loading &&
                        conversations.length === 0 && (

                            <div className="history-empty">

                                Your saved chats will show up here.

                            </div>

                        )}

                </div>


                <div className="assistant-history-foot">

                    <span className="ai-status-dot" />

                    Conversations securely stored in MongoDB

                </div>

            </aside>


            {/* =====================================================
          MAIN AI CHAT
      ===================================================== */}

            <section className="assistant-main panel">


                {/* ===================================================
            HEADER
        =================================================== */}

                <header className="assistant-chat-header">

                    <div className="assistant-bot-avatar">

                        <Bot size={21} />

                    </div>


                    <div>

                        <h2>
                            {config.title}
                        </h2>

                        <p>

                            <span className="ai-status-dot" />

                            {config.subtitle}

                        </p>

                    </div>


                    <button
                        className="icon-btn"
                        onClick={() =>
                            activeId &&
                            openConversation(activeId)
                        }
                        title="Reload conversation"
                    >

                        <RotateCcw size={17} />

                    </button>

                </header>


                {/* ===================================================
            MESSAGES
        =================================================== */}

                <div className="assistant-messages">

                    {messages.map(
                        (m, index) => (

                            <div
                                key={`${index}-${m.role}`}
                                className={
                                    `chat-row ${m.role === "user"
                                        ? "chat-row-user"
                                        : ""
                                    }`
                                }
                            >


                                {m.role === "assistant" && (

                                    <div className="chat-avatar chat-ai-avatar">

                                        <Sparkles size={14} />

                                    </div>

                                )}


                                <div
                                    className={
                                        `chat-bubble ${m.role === "user"
                                            ? "chat-user"
                                            : "chat-ai"
                                        }`
                                    }
                                >

                                    {m.content
                                        .split("\n")
                                        .map(
                                            (line, i) => (

                                                <p key={i}>

                                                    {line || <br />}

                                                </p>

                                            )
                                        )}


                                    {m.role === "assistant" &&
                                        index > 0 && (

                                            <span className="chat-message-meta">

                                                {config.meta}

                                            </span>

                                        )}

                                </div>


                                {m.role === "user" && (

                                    <div className="chat-avatar chat-user-avatar">

                                        <UserRound size={14} />

                                    </div>

                                )}

                            </div>

                        )
                    )}


                    {/* =================================================
              TYPING INDICATOR
          ================================================= */}

                    {sending && (

                        <div className="chat-row">

                            <div className="chat-avatar chat-ai-avatar">

                                <Sparkles size={14} />

                            </div>


                            <div className="chat-bubble chat-ai">

                                <div className="typing-dots">

                                    <i />

                                    <i />

                                    <i />

                                </div>

                            </div>

                        </div>

                    )}


                    <div ref={bottomRef} />

                </div>


                {/* ===================================================
            COMPOSE AREA
        =================================================== */}

                <div className="assistant-compose">


                    {/* =================================================
              ROLE-BASED QUICK PROMPTS
          ================================================= */}

                    <div className="prompt-suggestions">

                        {config.prompts.map(
                            (p) => (

                                <button
                                    key={p}
                                    onClick={() =>
                                        send(p)
                                    }
                                    disabled={sending}
                                >

                                    <Sparkles size={12} />

                                    {p}

                                </button>

                            )
                        )}

                    </div>


                    {/* =================================================
              INPUT
          ================================================= */}

                    <form
                        onSubmit={(e) => {

                            e.preventDefault();

                            send();

                        }}
                        className="assistant-input-row"
                    >

                        <textarea
                            ref={inputRef}
                            value={input}
                            onChange={(e) =>
                                setInput(e.target.value)
                            }
                            placeholder={
                                role === "Student"
                                    ? "Ask a course question…"
                                    : role === "Trainer"
                                        ? "Ask your AI teaching assistant…"
                                        : "Ask your AI LMS assistant…"
                            }
                            rows={1}
                            onKeyDown={(e) => {

                                if (
                                    e.key === "Enter" &&
                                    !e.shiftKey
                                ) {

                                    e.preventDefault();

                                    send();

                                }

                            }}
                        />


                        <button
                            disabled={
                                sending ||
                                !input.trim()
                            }
                            className="assistant-send"
                            aria-label="Send message"
                        >

                            <ArrowUp size={19} />

                        </button>

                    </form>


                    {/* =================================================
              DISCLAIMER
          ================================================= */}

                    <div className="assistant-disclaimer">

                        AI can make mistakes. Check important
                        information against your course material.

                    </div>

                </div>

            </section>

        </div>

    );

}