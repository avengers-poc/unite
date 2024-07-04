import { useRef, useState } from "react";
import { Box, Flex } from "@infotrack/zenith-ui";
import Messages from "../messages";
import * as css from "./chatCss";
import axios from "axios";

export interface ChatRequest {
  message?: string | undefined;
}

export interface IMessage {
  data: string;
  isBot: boolean;
}

export default function Chat() {
  const [messages, setMessages] = useState<IMessage[]>([]);
  const [newMessage, setNewMessage] = useState("");
  const messagesRef = useRef<IMessage[]>([]);
  messagesRef.current = messages;

  function onSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setMessages([...messagesRef.current, { data: newMessage, isBot: false }]);
    setNewMessage("");

    void axios
      .post(
        `http://localhost:5000/Chat/SendMessage?message=${newMessage}&threadId=thread_UmLyDwOjA0dFi6uYzO8iifKf`
      )
      .then((response) => {
        setMessages([
          ...messagesRef.current,
          {
            data: response.data.response,
            isBot: true,
          },
        ]);
      })
      .catch((error) => console.error("Error fetching messages:", error));
  }

  return (
    <Flex sx={css.chatContent}>
      <div>
        <Messages messages={messages} />
      </div>
      <Box sx={css.inputMessage}>
        <form className="form" onSubmit={(e) => onSubmit(e)}>
          <input
            className="input"
            onChange={(e) => setNewMessage(e.target.value)}
            value={newMessage}
            type="text"
            placeholder="Enter your message and press ENTER"
            autoFocus
          />
          <button className="button">Send</button>
        </form>
      </Box>
    </Flex>
  );
}
