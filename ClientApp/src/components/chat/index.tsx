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
  const [threadId, setThreadId] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const messagesRef = useRef<IMessage[]>([]);
  messagesRef.current = messages;

  function onSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();

    if (newMessage) {
      setMessages([...messagesRef.current, { data: newMessage, isBot: false }]);
      setNewMessage("");
      setIsLoading(true);
      let url: string = `http://localhost:5000/Chat/SendMessage?message=${newMessage}`;
      if (threadId) {
        url += `&threadId=${threadId}`;
      }
      void axios
        .post(url)
        .then((response) => {
          setThreadId(response.data.threadId);
          setMessages([
            ...messagesRef.current,
            {
              data: response.data.response,
              isBot: true,
            },
          ]);
        })
        .catch((error) => console.error("Error fetching messages:", error))
        .finally(() => setIsLoading(false));
    }
  }

  return (
    <Flex sx={css.chatContent}>
      <Box sx={css.messageContent}>
        <Messages messages={messages} />
      </Box>
      <Box sx={css.inputMessage(isLoading)}>
        <form className="form" onSubmit={(e) => onSubmit(e)}>
          <input
            className="input"
            onChange={(e) => setNewMessage(e.target.value)}
            value={newMessage}
            type="text"
            placeholder={
              isLoading
                ? "the-avengers-bot is progressing your message"
                : "Enter your message and press ENTER"
            }
            autoFocus
            disabled={isLoading}
          />
          <button disabled={isLoading} className="button">
            Send
          </button>
        </form>
      </Box>
    </Flex>
  );
}
