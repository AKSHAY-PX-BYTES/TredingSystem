import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiApiService } from '../../services/ai-api.service';
import { AiSignalDto, ChatRequest } from '../../models/models';

interface ChatMsg { isUser: boolean; content: string; suggestions?: string[]; signals?: AiSignalDto[]; }

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="chat-page">
      <div class="chat-container">
        <div class="chat-header"><h2>🤖 AI Trading Assistant</h2><p>Ask about stocks, earnings, options strategies, and more</p></div>
        <div class="chat-messages">
          <div *ngIf="messages.length === 0" class="chat-welcome">
            <div class="welcome-icon">🤖</div><h3>Welcome to AI Trading Chat</h3>
            <div class="suggestion-chips">
              <button class="chip" (click)="sendSuggestion('What stocks should I buy?')">What stocks should I buy?</button>
              <button class="chip" (click)="sendSuggestion('Explain this earnings report')">Explain earnings</button>
              <button class="chip" (click)="sendSuggestion('Options strategy recommendations')">Options strategies</button>
            </div>
          </div>
          <div *ngFor="let msg of messages" class="chat-message" [class.user-message]="msg.isUser" [class.assistant-message]="!msg.isUser">
            <div class="message-avatar">{{msg.isUser ? '👤' : '🤖'}}</div>
            <div class="message-bubble">
              <div class="message-content" [innerHTML]="formatMessage(msg.content)"></div>
              <div class="message-suggestions" *ngIf="msg.suggestions?.length">
                <button class="chip" *ngFor="let s of msg.suggestions" (click)="sendSuggestion(s)">{{s}}</button>
              </div>
              <div class="message-signals" *ngIf="msg.signals?.length">
                <div *ngFor="let sig of msg.signals" class="mini-signal" [class]="'signal-badge-' + sig.signalType.toLowerCase()">
                  <strong>{{sig.symbol}}</strong> {{sig.signalType}} ({{sig.confidence}}%)
                </div>
              </div>
            </div>
          </div>
          <div *ngIf="isTyping" class="chat-message assistant-message">
            <div class="message-avatar">🤖</div>
            <div class="message-bubble typing"><span class="dot"></span><span class="dot"></span><span class="dot"></span></div>
          </div>
        </div>
        <div class="chat-input-area">
          <input [(ngModel)]="inputMessage" (keypress)="handleKeyPress($event)" placeholder="Ask about stocks, strategies, earnings..." class="chat-input" [disabled]="isTyping" />
          <button class="chat-send-btn" (click)="sendMessage()" [disabled]="isTyping || !inputMessage.trim()">➤</button>
        </div>
      </div>
    </div>
  `
})
export class ChatComponent {
  messages: ChatMsg[] = [];
  inputMessage = '';
  sessionId: string | undefined;
  isTyping = false;

  constructor(private aiApi: AiApiService) {}

  handleKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter') this.sendMessage();
  }

  sendSuggestion(text: string): void {
    this.inputMessage = text;
    this.sendMessage();
  }

  sendMessage(): void {
    if (!this.inputMessage.trim() || this.isTyping) return;
    const userMsg = this.inputMessage.trim();
    this.inputMessage = '';
    this.messages.push({ isUser: true, content: userMsg });
    this.isTyping = true;

    const req: ChatRequest = { message: userMsg, sessionId: this.sessionId };
    this.aiApi.sendChat(req).subscribe(response => {
      this.isTyping = false;
      if (response) {
        this.sessionId = response.sessionId;
        this.messages.push({ isUser: false, content: response.response, suggestions: response.suggestions || undefined, signals: response.relatedSignals || undefined });
      } else {
        this.messages.push({ isUser: false, content: 'Sorry, I encountered an error. Please try again.' });
      }
    });
  }

  formatMessage(content: string): string {
    return content.replace(/\n\n/g, '<br/><br/>').replace(/\n/g, '<br/>');
  }
}
