import { Box } from "@infotrack/zenith-ui";
import React, { useEffect, useRef } from "react";
import { IMessage } from "../chat";
import * as css from "./messagesCss";
import parse from "html-react-parser";
import Markdown from "react-markdown";

export default function Messages({ messages }: { messages: IMessage[] }) {
  const bottomRef = useRef<HTMLElement>(null);
  useEffect(() => {
    if (bottomRef && bottomRef.current) {
      bottomRef.current.scrollIntoView({ behavior: "smooth" });
    }
  });
  return (
    <Box sx={css.messagesList}>
      {messages.map((message, index) => Message(message, index))}
      <Box ref={bottomRef}></Box>
    </Box>
  );
}

function Message(message: IMessage, id: number) {
  const newlineText = <Markdown>{message.data}</Markdown>;
  return (
    <Box sx={css.messageItem(message.isBot)} key={id}>
      <Box sx={css.avatar(message.isBot)} className="avatar" />
      <Box sx={css.messageContent}>
        <Box sx={css.username}>{message.isBot ? "the-avengers-bot" : "me"}</Box>
        <Box sx={css.text}>{newlineText}</Box>
      </Box>
    </Box>
  );
}
