import SwiftUI

struct ChatView: View {
    @State private var messages: [ChatMessageDto] = []
    @State private var currentMessage = ""
    @State private var isLoading = true
    @State private var isSending = false
    @State private var errorMessage: String?

    var body: some View {
        VStack(spacing: 0) {
            ScrollViewReader { proxy in
                ScrollView {
                    LazyVStack(alignment: .leading, spacing: 10) {
                        if isLoading {
                            ProgressView()
                                .padding()
                        } else if messages.isEmpty {
                            Text("Frag mich etwas zu deinen Gesundheitsdaten — ich kenne deine aktuellen Werte aus den letzten 7 Tagen.")
                                .font(.callout)
                                .foregroundStyle(.secondary)
                                .padding()
                        }

                        ForEach(messages) { message in
                            ChatBubble(message: message)
                                .id(message.id)
                        }

                        if isSending {
                            HStack {
                                ProgressView()
                                Text("Denkt nach…")
                                    .font(.footnote)
                                    .foregroundStyle(.secondary)
                            }
                            .padding(.horizontal)
                        }
                    }
                    .padding(.vertical)
                }
                .onChange(of: messages.count) { _, _ in
                    if let last = messages.last {
                        withAnimation {
                            proxy.scrollTo(last.id, anchor: .bottom)
                        }
                    }
                }
            }

            if let errorMessage {
                Text(errorMessage)
                    .font(.footnote)
                    .foregroundStyle(.red)
                    .padding(.horizontal)
                    .padding(.bottom, 4)
            }

            HStack {
                TextField("Nachricht schreiben…", text: $currentMessage)
                    .textFieldStyle(.roundedBorder)
                    .disabled(isSending)
                Button {
                    Task { await sendMessage() }
                } label: {
                    Image(systemName: "arrow.up.circle.fill")
                        .font(.title2)
                }
                .disabled(isSending || currentMessage.trimmingCharacters(in: .whitespaces).isEmpty)
            }
            .padding()
        }
        .navigationTitle("KI-Coach")
        .task {
            await loadHistory()
        }
    }

    private func loadHistory() async {
        isLoading = true
        defer { isLoading = false }
        do {
            messages = try await ChatService.getHistory()
        } catch {
            errorMessage = "Laden fehlgeschlagen: \(error.localizedDescription)"
        }
    }

    private func sendMessage() async {
        let text = currentMessage.trimmingCharacters(in: .whitespaces)
        guard !text.isEmpty, !isSending else { return }

        currentMessage = ""
        isSending = true
        errorMessage = nil
        messages.append(ChatMessageDto(role: "user", content: text, createdAt: "\(Date().timeIntervalSince1970)"))

        do {
            let response = try await ChatService.sendMessage(text)
            messages.append(ChatMessageDto(role: "assistant", content: response.answer, createdAt: response.createdAt))
        } catch {
            errorMessage = "Fehlgeschlagen: \(error.localizedDescription)"
        }

        isSending = false
    }
}

private struct ChatBubble: View {
    let message: ChatMessageDto

    var body: some View {
        HStack {
            if message.isUser {
                Spacer(minLength: 40)
            }

            Text(message.content)
                .padding(.horizontal, 14)
                .padding(.vertical, 10)
                .background(message.isUser ? Color.accentColor : Color(.secondarySystemBackground))
                .foregroundStyle(message.isUser ? .white : .primary)
                .clipShape(RoundedRectangle(cornerRadius: 16))

            if !message.isUser {
                Spacer(minLength: 40)
            }
        }
        .padding(.horizontal)
    }
}

#Preview {
    NavigationStack { ChatView() }
}
