import { useEffect, useRef, useState } from "react";
import { Box, Flex } from "@infotrack/zenith-ui";
import Messages from "../messages";
import * as css from "./chatCss";

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
  const [botAnswer, setBotAnswer] = useState(false);
  const messagesRef = useRef<IMessage[]>([]);
  messagesRef.current = messages;

  //   useEffect(() => {
  //     // Fetch messages from the server (replace with your backend endpoint)
  //     axios
  //       .get("/api/messages")
  //       .then((response) => setMessages(response.data))
  //       .catch((error) => console.error("Error fetching messages:", error));
  //   }, []);

  const handleSendMessage = (message: string) => {
    setNewMessage(message);

    // Send the new message to the server (replace with your backend endpoint)
    // axios
    //   .post("/api/messages", { text: newMessage })
    //   .then((response) => {
    //     setMessages([...messages, response.data]);
    //     setNewMessage("");
    //   })
    //   .catch((error) => console.error("Error sending message:", error));
  };

  function onSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setMessages([...messagesRef.current, { data: newMessage, isBot: false }]);
    setNewMessage("");
    setBotAnswer(true);

    // onSendMessage(text);
  }

  useEffect(() => {
    if (botAnswer) {
      setTimeout(() => {
        setMessages([
          ...messagesRef.current,
          {
            data: `<p>Chromium, the open-source browser project that forms the basis of Google Chrome, handles MP4 video files using a combination of several components and processes. Here's a simplified overview of how Chromium runs MP4 videos:</p><h3>1. <strong>Network Layer</strong></h3><p>When you navigate to a webpage containing an MP4 video, the browser's network layer fetches the video data from the server using protocols like HTTP or HTTPS.</p><h3>2. <strong>Media Pipeline</strong></h3><p>Chromium has a dedicated media pipeline responsible for handling multimedia content. This pipeline includes several components:</p><ul><li><strong>Demuxer</strong>: The demuxer reads the MP4 file and extracts its tracks (e.g., video, audio, subtitles).</li><li><strong>Decoder</strong>: The decoder takes compressed video and audio data from the demuxer and decompresses it. Chromium supports various codecs, such as H.264 for video and AAC for audio, depending on the platform and available codecs.</li></ul><h3>3. <strong>Rendering Pipeline</strong></h3><p>Once the video and audio tracks are decoded, the rendering pipeline takes over:</p>`,
            isBot: true,
          },
        ]);
      }, 2000);
      setBotAnswer(false);
    }
  }, [botAnswer]);

  return (
    <Flex sx={css.chatContent}>
      <div>
        <Messages messages={messages} />
      </div>
      <Box sx={css.inputMessage}>
        <form className="form" onSubmit={(e) => onSubmit(e)}>
          <input
            className="input"
            onChange={(e) => handleSendMessage(e.target.value)}
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
